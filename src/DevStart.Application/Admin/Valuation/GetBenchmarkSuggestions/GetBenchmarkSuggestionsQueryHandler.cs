using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Scoring.Benchmarks;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Valuation.GetBenchmarkSuggestions
{
    /// <summary>
    /// Loads staging, hands it to the pure <see cref="BenchmarkDerivationEngine"/>, and decorates the
    /// result with what is on file today. This handler is the only part that touches the database; the
    /// arithmetic it wraps has no idea a database exists.
    /// </summary>
    internal sealed class GetBenchmarkSuggestionsQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetBenchmarkSuggestionsQuery, BenchmarkSuggestionsResponse>
    {
        public async Task<Result<BenchmarkSuggestionsResponse>> Handle(
            GetBenchmarkSuggestionsQuery query,
            CancellationToken cancellationToken)
        {
            DateTime asOf = query.AsOf ?? dateTimeProvider.UtcNow;
            BenchmarkDerivationParameters defaults = BenchmarkDerivationParameters.Defaults(asOf);

            var parameters = new BenchmarkDerivationParameters(
                MinComparables: query.MinComparables ?? defaults.MinComparables,
                CountryDiscount: query.CountryDiscount ?? defaults.CountryDiscount,
                IlliquidityAndSizeDiscount: query.IlliquidityAndSizeDiscount ?? defaults.IlliquidityAndSizeDiscount,
                DatasetRegion: string.IsNullOrWhiteSpace(query.DatasetRegion)
                    ? defaults.DatasetRegion
                    : query.DatasetRegion.Trim(),
                AsOf: asOf);

            DateTime quarterStart = BenchmarkQuarter.StartOf(asOf);

            List<BenchmarkIssuer> issuers = await context.BenchmarkIssuers
                .AsNoTracking()
                .Where(i => i.IsActive)
                .ToListAsync(cancellationToken);

            // Staging is tiny (tens of issuers × a few metrics × a few quarters), so one pull and an
            // in-memory reduction beats a query per sector.
            List<BenchmarkObservation> observations = await context.BenchmarkObservations
                .AsNoTracking()
                .Where(o => o.AsOf <= quarterStart)
                .ToListAsync(cancellationToken);

            Dictionary<string, Industry?> mappings = await context.BenchmarkIndustryMappings
                .AsNoTracking()
                .Where(m => m.SourceKind == BenchmarkMappingSourceKind.Damodaran)
                .ToDictionaryAsync(m => m.ExternalKey, m => m.Industry, StringComparer.OrdinalIgnoreCase, cancellationToken);

            List<DamodaranBucketInput> buckets = observations
                .Where(o => o.Source == BenchmarkObservationSource.Damodaran
                    && o.Metric == BenchmarkObservationMetric.EvSales
                    && o.ExternalKey != null)
                .Select(o => new DamodaranBucketInput(o.ExternalKey!, o.Value, o.AsOf.Year, o.DatasetRegion))
                .ToList();

            List<ComparableInput> comparables = BuildComparables(issuers, observations, quarterStart);

            IReadOnlyList<BenchmarkSuggestion> derived = BenchmarkDerivationEngine.Derive(
                new BenchmarkDerivationInputs(buckets, mappings, comparables),
                parameters);

            (Dictionary<(BenchmarkMetricType, Industry), decimal> current,
                HashSet<(BenchmarkMetricType, Industry)> collisions) =
                    await LoadCurrentSectorRowsAsync(asOf, quarterStart, cancellationToken);

            DamodaranBucketInput? newestBucket = buckets
                .OrderByDescending(b => b.DatasetYear)
                .FirstOrDefault();

            return new BenchmarkSuggestionsResponse
            {
                MinComparables = parameters.MinComparables,
                CountryDiscount = parameters.CountryDiscount,
                IlliquidityAndSizeDiscount = parameters.IlliquidityAndSizeDiscount,
                DatasetRegion = parameters.DatasetRegion,
                AsOf = asOf,
                QuarterLabel = BenchmarkQuarter.Label(quarterStart),
                HasObservations = observations.Count > 0,
                LastMarketCapCollectedAt = LastFetch(observations, BenchmarkObservationMetric.MarketCap),
                LastRevenueCollectedAt = LastFetch(observations, BenchmarkObservationMetric.Revenue),
                DamodaranDatasetYear = newestBucket?.DatasetYear,
                DamodaranDatasetRegion = newestBucket?.Region,
                Suggestions = derived.Select(s => Map(s, current, collisions)).ToList(),
            };
        }

        private static List<ComparableInput> BuildComparables(
            List<BenchmarkIssuer> issuers,
            List<BenchmarkObservation> observations,
            DateTime quarterStart)
        {
            Dictionary<Guid, List<BenchmarkObservation>> byIssuer = observations
                .Where(o => o.IssuerId != null)
                .GroupBy(o => o.IssuerId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var comparables = new List<ComparableInput>();

            foreach (BenchmarkIssuer issuer in issuers)
            {
                if (!byIssuer.TryGetValue(issuer.Id, out List<BenchmarkObservation>? mine))
                {
                    // A manual override alone is still only half a multiple — without a capitalisation
                    // there is nothing to divide.
                    continue;
                }

                BenchmarkObservation? cap = mine
                    .Where(o => o.Metric == BenchmarkObservationMetric.MarketCap && o.AsOf <= quarterStart)
                    .OrderByDescending(o => o.AsOf)
                    .FirstOrDefault();

                if (cap is null || cap.Value <= 0m)
                {
                    continue;
                }

                // The override wins over anything collected, and says so downstream.
                decimal? revenue = issuer.RevenueOverride;
                int? fiscalYear = issuer.RevenueOverrideFiscalYear;
                bool manual = revenue is > 0m;

                if (!manual)
                {
                    BenchmarkObservation? collected = mine
                        .Where(o => o.Metric == BenchmarkObservationMetric.Revenue && o.Value > 0m)
                        .OrderByDescending(o => o.FiscalYear ?? 0)
                        .ThenByDescending(o => o.AsOf)
                        .FirstOrDefault();

                    revenue = collected?.Value;
                    fiscalYear = collected?.FiscalYear;
                }

                if (revenue is not > 0m)
                {
                    continue;
                }

                comparables.Add(new ComparableInput(
                    issuer.Ticker, issuer.Industry, cap.Value, revenue.Value, fiscalYear, manual));
            }

            return comparables;
        }

        /// <summary>
        /// Current effective values and would-be duplicates for the two sector-only metrics. The
        /// collision set is what turns the append-only duplicate rule into a warning shown before the
        /// click instead of a 409 after it.
        /// </summary>
        private async Task<(Dictionary<(BenchmarkMetricType, Industry), decimal> Current,
                HashSet<(BenchmarkMetricType, Industry)> Collisions)>
            LoadCurrentSectorRowsAsync(DateTime asOf, DateTime quarterStart, CancellationToken cancellationToken)
        {
            List<ValuationBenchmark> rows = await context.ValuationBenchmarks
                .AsNoTracking()
                .Where(b => b.MetricType == BenchmarkMetricType.RevenueMultiple
                    || b.MetricType == BenchmarkMetricType.CompetitionIntensity)
                .ToListAsync(cancellationToken);

            Dictionary<(BenchmarkMetricType, Industry), decimal> current = rows
                .Where(b => b.EffectiveFrom <= asOf)
                .GroupBy(b => (b.MetricType, b.Industry))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.EffectiveFrom).First().Value);

            HashSet<(BenchmarkMetricType, Industry)> collisions = rows
                .Where(b => b.Stage == null && b.EffectiveFrom == quarterStart)
                .Select(b => (b.MetricType, b.Industry))
                .ToHashSet();

            return (current, collisions);
        }

        private static DateTime? LastFetch(
            List<BenchmarkObservation> observations, BenchmarkObservationMetric metric)
        {
            List<BenchmarkObservation> ofMetric = observations.Where(o => o.Metric == metric).ToList();
            return ofMetric.Count == 0 ? null : ofMetric.Max(o => o.FetchedAt);
        }

        private static BenchmarkSuggestionResponse Map(
            BenchmarkSuggestion suggestion,
            Dictionary<(BenchmarkMetricType, Industry), decimal> current,
            HashSet<(BenchmarkMetricType, Industry)> collisions)
        {
            (BenchmarkMetricType, Industry) key = (suggestion.MetricType, suggestion.Industry);
            decimal? currentValue = current.TryGetValue(key, out decimal value) ? value : null;

            decimal? delta = suggestion.Value is { } suggested && currentValue is { } existing && existing != 0m
                ? Math.Round((suggested - existing) / existing * 100m, 1, MidpointRounding.AwayFromZero)
                : null;

            return new BenchmarkSuggestionResponse
            {
                MetricType = suggestion.MetricType,
                Industry = suggestion.Industry,
                Value = suggestion.Value,
                ComparableCount = suggestion.ComparableCount,
                IsDerived = suggestion.IsDerived,
                Chain = [.. suggestion.Chain],
                FiscalYears = [.. suggestion.FiscalYears],
                Source = suggestion.Source,
                NoSuggestionReason = suggestion.NoSuggestionReason,
                EffectiveFrom = suggestion.EffectiveFrom,
                CurrentValue = currentValue,
                DeltaPercent = delta,
                CollidesWithExisting = collisions.Contains(key),
            };
        }
    }
}
