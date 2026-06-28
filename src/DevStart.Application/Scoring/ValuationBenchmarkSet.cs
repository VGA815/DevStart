using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;

namespace DevStart.Application.Scoring
{
    /// <summary>
    /// One benchmark row, flattened for transport/caching and as-of resolution. Mirrors
    /// <see cref="ValuationBenchmark"/> minus the audit/source fields the engine does not read.
    /// </summary>
    public sealed record ValuationBenchmarkRow(
        BenchmarkMetricType MetricType,
        Industry Industry,
        StartupStage? Stage,
        decimal Value,
        DateTime EffectiveFrom);

    /// <summary>
    /// An immutable, as-of snapshot of the valuation benchmarks the engine reads: pre-money medians by
    /// sector/stage and revenue multiples by sector. Built from the versioned rows by keeping, per key,
    /// the latest version with <c>EffectiveFrom ≤ asOf</c>. A missing value is an explicit "no data"
    /// signal (<c>null</c>) — the consuming method then drops out of the ensemble.
    /// </summary>
    public sealed class ValuationBenchmarkSet
    {
        private readonly IReadOnlyDictionary<(Industry Industry, StartupStage Stage), decimal> _medians;
        private readonly IReadOnlyDictionary<Industry, decimal> _revenueMultiples;

        public ValuationBenchmarkSet(
            IReadOnlyDictionary<(Industry Industry, StartupStage Stage), decimal> medians,
            IReadOnlyDictionary<Industry, decimal> revenueMultiples)
        {
            _medians = medians;
            _revenueMultiples = revenueMultiples;
        }

        /// <summary>An empty set — every lookup returns "no data".</summary>
        public static readonly ValuationBenchmarkSet Empty = new(
            new Dictionary<(Industry, StartupStage), decimal>(),
            new Dictionary<Industry, decimal>());

        /// <summary>
        /// Resolves the rows as of <paramref name="asOf"/>: for each key, the value of the latest
        /// version whose <c>EffectiveFrom ≤ asOf</c>. Future-dated versions are ignored.
        /// </summary>
        public static ValuationBenchmarkSet FromRows(IEnumerable<ValuationBenchmarkRow> rows, DateTime asOf)
        {
            var medians = new Dictionary<(Industry, StartupStage), decimal>();
            var medianEffective = new Dictionary<(Industry, StartupStage), DateTime>();
            var multiples = new Dictionary<Industry, decimal>();
            var multipleEffective = new Dictionary<Industry, DateTime>();

            foreach (ValuationBenchmarkRow row in rows)
            {
                if (row.EffectiveFrom > asOf)
                {
                    continue;
                }

                if (row.MetricType == BenchmarkMetricType.PreMoneyMedian && row.Stage is { } stage)
                {
                    (Industry, StartupStage) key = (row.Industry, stage);
                    if (!medianEffective.TryGetValue(key, out DateTime current) || row.EffectiveFrom > current)
                    {
                        medianEffective[key] = row.EffectiveFrom;
                        medians[key] = row.Value;
                    }
                }
                else if (row.MetricType == BenchmarkMetricType.RevenueMultiple)
                {
                    if (!multipleEffective.TryGetValue(row.Industry, out DateTime current) || row.EffectiveFrom > current)
                    {
                        multipleEffective[row.Industry] = row.EffectiveFrom;
                        multiples[row.Industry] = row.Value;
                    }
                }
            }

            return new ValuationBenchmarkSet(medians, multiples);
        }

        /// <summary>
        /// Median pre-money valuation (RUB): the sector-specific value when present, otherwise the
        /// general (<see cref="Industry.Other"/>) stage median; <c>null</c> when neither exists.
        /// </summary>
        public decimal? Median(Industry industry, StartupStage stage)
        {
            if (industry != Industry.Other && _medians.TryGetValue((industry, stage), out decimal sector))
            {
                return sector;
            }
            return _medians.TryGetValue((Industry.Other, stage), out decimal stageOnly) ? stageOnly : null;
        }

        /// <summary>Whether a sector-specific (non-<see cref="Industry.Other"/>) median backs the lookup.</summary>
        public bool HasSectorMedian(Industry industry, StartupStage stage) =>
            industry != Industry.Other && _medians.ContainsKey((industry, stage));

        /// <summary>EV/Revenue multiple for the sector; <c>null</c> when none is on file.</summary>
        public decimal? RevenueMultiple(Industry industry) =>
            _revenueMultiples.TryGetValue(industry, out decimal m) ? m : null;
    }

    /// <summary>
    /// Single read point for the valuation benchmarks. Both consumers (Scorecard median, Comparable
    /// multiple) go through here, so the engine is isolated from storage and a future own-data overlay
    /// (variant E, N=10) can slot in behind this abstraction without touching the methods.
    /// </summary>
    public interface IValuationBenchmarkProvider
    {
        Task<ValuationBenchmarkSet> GetAsync(DateTime asOf, CancellationToken cancellationToken);
    }
}
