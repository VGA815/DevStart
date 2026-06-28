using DevStart.Domain.Startups;
using DevStart.SharedKernel;

namespace DevStart.Domain.Valuation
{
    /// <summary>
    /// A single versioned valuation benchmark — either a pre-money median (per sector/stage) or a
    /// revenue multiple (per sector). Data lives in the database (not in code) so it can be refreshed
    /// without a release; rows are append-only and versioned by <see cref="EffectiveFrom"/>, so a
    /// valuation can be reproduced as of any past date by picking the latest version ≤ that date.
    /// <see cref="Source"/> is mandatory for methodology transparency.
    /// </summary>
    public sealed class ValuationBenchmark : Entity
    {
        public Guid Id { get; set; }

        public BenchmarkMetricType MetricType { get; set; }

        /// <summary>Sector. <see cref="Industry.Other"/> carries the general (stage-only) median.</summary>
        public Industry Industry { get; set; }

        /// <summary>Stage for medians; <c>null</c> for revenue multiples (sector-only).</summary>
        public StartupStage? Stage { get; set; }

        /// <summary>Median pre-money valuation (RUB) or the revenue multiple, per <see cref="MetricType"/>.</summary>
        public decimal Value { get; set; }

        /// <summary>Currency for medians (<c>"RUB"</c>); <c>null</c> for the dimensionless multiplier.</summary>
        public string? Currency { get; set; }

        /// <summary>Start of this version's validity; the engine reads the latest version ≤ valuation date.</summary>
        public DateTime EffectiveFrom { get; set; }

        /// <summary>Provenance of the figure (e.g. "Dsight 2025 Annual" + link). Mandatory.</summary>
        public string Source { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        /// <summary>Admin who entered the row; <c>null</c> for the initial seed.</summary>
        public Guid? CreatedByUserId { get; set; }

        public ValuationBenchmark() { }

        public static ValuationBenchmark Create(
            BenchmarkMetricType metricType,
            Industry industry,
            StartupStage? stage,
            decimal value,
            string? currency,
            DateTime effectiveFrom,
            string source,
            Guid? createdByUserId,
            DateTime utcNow)
            => new()
            {
                Id = Guid.NewGuid(),
                MetricType = metricType,
                Industry = industry,
                Stage = stage,
                Value = value,
                Currency = currency,
                EffectiveFrom = effectiveFrom,
                Source = source,
                CreatedByUserId = createdByUserId,
                CreatedAt = utcNow,
            };
    }
}
