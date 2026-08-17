using System.Globalization;
using System.Text;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;

namespace DevStart.Application.Scoring.Benchmarks
{
    /// <summary>
    /// Turns staged observations into <c>RevenueMultiple</c> suggestions with the full trail of how each
    /// one got its value.
    ///
    /// The chain:
    /// <code>
    /// Damodaran base multiple for the sector
    ///   × Russian country coefficient
    ///         ≥ N comparables → median(market cap ÷ revenue) ÷ the Damodaran figure
    ///         otherwise       → the parameter
    ///   × illiquidity-and-size discount for private early-stage companies
    ///   = suggested RevenueMultiple
    /// </code>
    ///
    /// Four properties this type must keep:
    /// <list type="number">
    ///   <item>Pure. Staging and parameters in, suggestions out, no I/O and no clock.</item>
    ///   <item>Writes nothing — not to <c>valuation_benchmark</c>, not anywhere.</item>
    ///   <item>"No suggestion" is an explicit state, never a plausible-looking invented number.</item>
    ///   <item>The assembled source string is length-checked before the suggestion is offered.
    ///         A truncated justification is worse than a missing one: it still looks like a
    ///         justification.</item>
    /// </list>
    /// </summary>
    public static class BenchmarkDerivationEngine
    {
        /// <summary>Matches the <c>source</c> column and the add-command validator.</summary>
        public const int SourceMaxLength = 512;

        public static IReadOnlyList<BenchmarkSuggestion> Derive(
            BenchmarkDerivationInputs inputs,
            BenchmarkDerivationParameters parameters)
        {
            DateTime quarterStart = BenchmarkQuarter.StartOf(parameters.AsOf);
            string quarterLabel = BenchmarkQuarter.Label(quarterStart);

            // One dataset year at a time: mixing releases would make the base multiple a blend of two
            // vintages that no source string could honestly describe.
            List<DamodaranBucketInput> usableBuckets = SelectDataset(inputs.Buckets, parameters.DatasetRegion);
            int? datasetYear = usableBuckets.Count > 0 ? usableBuckets[0].DatasetYear : null;
            string? datasetRegion = usableBuckets.Count > 0 ? usableBuckets[0].Region : null;

            // Staging holds slices, but none under the requested name. Saying so beats reporting the
            // same "no mapped bucket" a genuinely unmapped sector gets — the fix is a different
            // parameter, not a mapping.
            string? datasetNote = usableBuckets.Count == 0 && inputs.Buckets.Count > 0
                ? $"срез «{parameters.DatasetRegion}» в staging отсутствует; загружены: "
                    + string.Join(", ", inputs.Buckets
                        .Select(b => b.Region ?? "(без среза)")
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(r => r, StringComparer.OrdinalIgnoreCase))
                : null;

            var suggestions = new List<BenchmarkSuggestion>();

            foreach (Industry industry in Enum.GetValues<Industry>())
            {
                suggestions.Add(DeriveOne(
                    industry, inputs, usableBuckets, datasetYear, datasetRegion, datasetNote,
                    parameters, quarterStart, quarterLabel));
            }

            suggestions.AddRange(DeriveCompetitionIntensity(quarterStart, quarterLabel));

            return suggestions;
        }

        /// <summary>
        /// Competition intensity for every sector, from the curated ranking in
        /// <see cref="CompetitionIntensityRanking"/>. Flagged as a parameter rather than derived,
        /// because that is what it is — a relative judgement with its basis written down, not a
        /// measurement. It travels the same acceptance path as everything else on this screen.
        /// </summary>
        private static IEnumerable<BenchmarkSuggestion> DeriveCompetitionIntensity(
            DateTime quarterStart, string quarterLabel)
        {
            IReadOnlyList<(Industry Industry, string Basis)> ranking = CompetitionIntensityRanking.Ranking;
            string spreadRule = CompetitionIntensityRanking.SpreadRule(ranking.Count);

            for (int i = 0; i < ranking.Count; i++)
            {
                (Industry industry, string basis) = ranking[i];
                int rank = i + 1;
                decimal value = CompetitionIntensityRanking.ValueForRank(rank);

                var chain = new List<DerivationStep>
                {
                    new("Ранг тесноты сектора", rank, $"{rank} из {ranking.Count}: {basis}"),
                    new("Раскладка ранга на шкалу", value, spreadRule),
                };

                // The rank denominator is in the string on purpose: it is what makes a later change to
                // the Industry enum visible as a change of scale rather than a silent re-basing.
                string source =
                    $"CompetitionIntensity {industry} {quarterLabel}. "
                    + $"Экспертное ранжирование {ranking.Count} секторов по тесноте для нового входа "
                    + $"(число активных игроков + барьеры входа); ранг {rank} из {ranking.Count}. "
                    + $"Раскладка: {spreadRule}. Основание: {basis}.";

                yield return source.Length > SourceMaxLength
                    ? new BenchmarkSuggestion
                    {
                        MetricType = BenchmarkMetricType.CompetitionIntensity,
                        Industry = industry,
                        ComparableCount = 0,
                        Chain = chain,
                        EffectiveFrom = quarterStart,
                        NoSuggestionReason =
                            $"обоснование не помещается в {SourceMaxLength} символов ({source.Length})",
                    }
                    : new BenchmarkSuggestion
                    {
                        MetricType = BenchmarkMetricType.CompetitionIntensity,
                        Industry = industry,
                        Value = value,
                        ComparableCount = 0,
                        IsDerived = false,
                        Chain = chain,
                        Source = source,
                        EffectiveFrom = quarterStart,
                    };
            }
        }

        private static BenchmarkSuggestion DeriveOne(
            Industry industry,
            BenchmarkDerivationInputs inputs,
            List<DamodaranBucketInput> usableBuckets,
            int? datasetYear,
            string? datasetRegion,
            string? datasetNote,
            BenchmarkDerivationParameters parameters,
            DateTime quarterStart,
            string quarterLabel)
        {
            var chain = new List<DerivationStep>();

            // --- Step 1: the Damodaran base for this sector ------------------------------------
            List<DamodaranBucketInput> sectorBuckets = usableBuckets
                .Where(b => inputs.BucketMappings.TryGetValue(b.ExternalKey, out Industry? mapped)
                    && mapped == industry)
                .ToList();

            decimal? damodaranBase = sectorBuckets.Count > 0
                ? Round2(Median(sectorBuckets.Select(b => b.EvSales)))
                : null;

            chain.Add(new DerivationStep(
                "Базовый мультипликатор Damodaran",
                damodaranBase,
                damodaranBase is null
                    ? datasetNote ?? "нет сопоставленных корзин"
                    : $"медиана EV/Sales по {sectorBuckets.Count} корзин(е/ам): "
                        + $"{string.Join(", ", sectorBuckets.Select(b => b.ExternalKey))}"));

            // --- Step 2: the Russian comparables ------------------------------------------------
            List<ComparableInput> comparables = inputs.Comparables
                .Where(c => c.Industry == industry && c.Revenue > 0m && c.MarketCap > 0m)
                .ToList();

            decimal? russianMedian = comparables.Count > 0
                ? Round2(Median(comparables.Select(c => c.MarketCap / c.Revenue)))
                : null;

            int[] fiscalYears = comparables
                .Select(c => c.FiscalYear)
                .Where(y => y.HasValue)
                .Select(y => y!.Value)
                .Distinct()
                .OrderBy(y => y)
                .ToArray();

            chain.Add(new DerivationStep(
                "Медиана по российским компараблам",
                russianMedian,
                comparables.Count == 0
                    ? "компараблов с обеими половинами мультипликатора нет"
                    : $"{comparables.Count} компараб(ов): "
                        + string.Join(", ", comparables.Select(DescribeComparable))));

            bool enoughComparables = comparables.Count >= parameters.MinComparables;

            // --- Step 3: base × country coefficient ---------------------------------------------
            decimal preDiscount;
            bool isDerived;
            string countryDetail;
            decimal countryCoefficient;

            if (damodaranBase is { } damodaran && enoughComparables && russianMedian is { } median)
            {
                // Both halves present: the coefficient is measured, not assumed. Multiplying it back
                // out lands on the Russian median exactly — which is the point, the Damodaran figure is
                // the yardstick the discount is expressed against, not an extra factor.
                countryCoefficient = Round2(median / damodaran);
                preDiscount = median;
                isDerived = true;
                countryDetail = $"выведен: {median:0.##}× ÷ {damodaran:0.##}×";
            }
            else if (damodaranBase is { } damodaranOnly)
            {
                countryCoefficient = parameters.CountryDiscount;
                preDiscount = damodaranOnly * countryCoefficient;
                isDerived = false;
                countryDetail = $"параметр: компараблов {comparables.Count} < {parameters.MinComparables}";
            }
            else if (enoughComparables && russianMedian is { } medianOnly)
            {
                // No mapped bucket, but enough Russian data to stand on its own. The coefficient is 1 by
                // construction: there is no foreign yardstick left to discount from.
                countryCoefficient = 1m;
                preDiscount = medianOnly;
                isDerived = true;
                countryDetail = "не применяется: база взята прямо из российских компараблов";
            }
            else
            {
                return NoSuggestion(
                    industry, comparables.Count, chain, fiscalYears, quarterStart,
                    $"компараблов {comparables.Count} < {parameters.MinComparables} и "
                        + (datasetNote ?? "нет сопоставленной корзины Damodaran"));
            }

            chain.Add(new DerivationStep("Страновой коэффициент РФ", countryCoefficient, countryDetail));

            // --- Step 4: illiquidity and size ----------------------------------------------------
            decimal value = Round2(preDiscount * parameters.IlliquidityAndSizeDiscount);

            chain.Add(new DerivationStep(
                "Дисконт за неликвидность и размер",
                parameters.IlliquidityAndSizeDiscount,
                "частные компании ранних стадий против публичных"));

            chain.Add(new DerivationStep("Предложенный RevenueMultiple", value, "итог цепочки"));

            if (value <= 0m)
            {
                return NoSuggestion(
                    industry, comparables.Count, chain, fiscalYears, quarterStart,
                    "цепочка дала неположительное значение");
            }

            // --- Step 5: the source string, checked before it is offered -------------------------
            string source = BuildSource(
                industry, quarterLabel, datasetYear, datasetRegion, sectorBuckets.Count, damodaranBase,
                countryCoefficient, isDerived, comparables.Count, russianMedian, fiscalYears,
                parameters.IlliquidityAndSizeDiscount, value);

            if (source.Length > SourceMaxLength)
            {
                return NoSuggestion(
                    industry, comparables.Count, chain, fiscalYears, quarterStart,
                    $"обоснование не помещается в {SourceMaxLength} символов ({source.Length}) — "
                        + "обрезать его нельзя, оно перестанет быть обоснованием");
            }

            return new BenchmarkSuggestion
            {
                MetricType = BenchmarkMetricType.RevenueMultiple,
                Industry = industry,
                Value = value,
                ComparableCount = comparables.Count,
                IsDerived = isDerived,
                Chain = chain,
                FiscalYears = fiscalYears,
                Source = source,
                EffectiveFrom = quarterStart,
            };
        }

        /// <summary>
        /// Keeps the newest release, and only the requested regional slice. A slice named in the
        /// parameters that is nowhere in staging yields no buckets — and therefore an honest "nothing to
        /// suggest" — rather than quietly falling back to whichever slice happens to be loaded.
        /// </summary>
        private static List<DamodaranBucketInput> SelectDataset(
            IReadOnlyList<DamodaranBucketInput> buckets, string datasetRegion)
        {
            IEnumerable<DamodaranBucketInput> candidates = buckets;

            if (!string.IsNullOrWhiteSpace(datasetRegion))
            {
                candidates = candidates.Where(b =>
                    string.Equals(b.Region, datasetRegion.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            List<DamodaranBucketInput> filtered = candidates.ToList();
            if (filtered.Count == 0)
            {
                return filtered;
            }

            int newestYear = filtered.Max(b => b.DatasetYear);
            return filtered.Where(b => b.DatasetYear == newestYear).ToList();
        }

        private static BenchmarkSuggestion NoSuggestion(
            Industry industry,
            int comparableCount,
            List<DerivationStep> chain,
            int[] fiscalYears,
            DateTime quarterStart,
            string reason)
            => new()
            {
                MetricType = BenchmarkMetricType.RevenueMultiple,
                Industry = industry,
                Value = null,
                ComparableCount = comparableCount,
                IsDerived = false,
                Chain = chain,
                FiscalYears = fiscalYears,
                Source = null,
                NoSuggestionReason = reason,
                EffectiveFrom = quarterStart,
            };

        private static string DescribeComparable(ComparableInput c)
        {
            string year = c.FiscalYear is { } fy ? $" FY{fy}" : string.Empty;
            string manual = c.RevenueIsManual ? " ручн." : string.Empty;
            return $"{c.Ticker} {Round2(c.MarketCap / c.Revenue):0.##}×{year}{manual}";
        }

        private static string BuildSource(
            Industry industry,
            string quarterLabel,
            int? datasetYear,
            string? datasetRegion,
            int bucketCount,
            decimal? damodaranBase,
            decimal countryCoefficient,
            bool isDerived,
            int comparableCount,
            decimal? russianMedian,
            int[] fiscalYears,
            decimal illiquidityDiscount,
            decimal value)
        {
            var builder = new StringBuilder();
            builder.Append(CultureInfo.InvariantCulture, $"RevenueMultiple {industry} {quarterLabel}. ");

            if (damodaranBase is { } damodaran)
            {
                builder.Append(CultureInfo.InvariantCulture,
                    $"База: Damodaran {datasetYear} {datasetRegion}, медиана EV/Sales {damodaran:0.##}× ")
                    .Append(CultureInfo.InvariantCulture, $"по {bucketCount} корз. ");
            }
            else
            {
                builder.Append("База: российские компараблы (корзина Damodaran не сопоставлена). ");
            }

            builder.Append(CultureInfo.InvariantCulture, $"Страновой к-т {countryCoefficient:0.##} ")
                .Append(isDerived ? "(выведен" : "(параметр")
                .Append(CultureInfo.InvariantCulture, $", компараблов {comparableCount}");

            if (russianMedian is { } median)
            {
                builder.Append(CultureInfo.InvariantCulture, $", медиана MOEX/ГИР БО {median:0.##}×");
            }

            if (fiscalYears.Length > 0)
            {
                builder.Append(CultureInfo.InvariantCulture, $", выручка FY{string.Join("/", fiscalYears)}");
            }

            builder.Append("). ")
                .Append(CultureInfo.InvariantCulture, $"Дисконт неликвидности и размера {illiquidityDiscount:0.##}. ")
                .Append(CultureInfo.InvariantCulture, $"Итог {value:0.##}×.");

            return builder.ToString();
        }

        private static decimal Median(IEnumerable<decimal> values)
        {
            decimal[] sorted = values.OrderBy(v => v).ToArray();
            int middle = sorted.Length / 2;

            return sorted.Length % 2 == 1
                ? sorted[middle]
                : (sorted[middle - 1] + sorted[middle]) / 2m;
        }

        private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
