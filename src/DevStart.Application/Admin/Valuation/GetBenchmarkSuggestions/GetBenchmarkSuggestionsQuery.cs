using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Scoring.Benchmarks;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;

namespace DevStart.Application.Admin.Valuation.GetBenchmarkSuggestions
{
    /// <summary>
    /// Read-only preview of what the derivation would suggest under the given parameters. Every
    /// parameter is an input of the request and none of them is persisted — changing a discount
    /// recomputes the preview and writes nothing.
    ///
    /// Deliberately not an <see cref="ICacheableQuery"/>: the response is a function of parameters the
    /// caller chooses, so caching it would either serve someone else's parameters or need a key that
    /// encodes all of them for no benefit — this is an admin screen refreshed by hand a few times a
    /// quarter.
    /// </summary>
    public sealed record GetBenchmarkSuggestionsQuery(
        int? MinComparables,
        decimal? CountryDiscount,
        decimal? IlliquidityAndSizeDiscount,
        string? DatasetRegion,
        DateTime? AsOf) : IQuery<BenchmarkSuggestionsResponse>;

    /// <summary>The preview, plus enough context for the screen's empty states to be specific.</summary>
    public sealed class BenchmarkSuggestionsResponse
    {
        public int MinComparables { get; init; }
        public decimal CountryDiscount { get; init; }
        public decimal IlliquidityAndSizeDiscount { get; init; }
        public string DatasetRegion { get; init; } = null!;
        public DateTime AsOf { get; init; }

        /// <summary>Quarter the suggestions describe, e.g. "2026Q3".</summary>
        public string QuarterLabel { get; init; } = null!;

        /// <summary>Whether staging holds anything at all — "run a collection" vs "not enough comparables".</summary>
        public bool HasObservations { get; init; }

        public DateTime? LastMarketCapCollectedAt { get; init; }
        public DateTime? LastRevenueCollectedAt { get; init; }
        public int? DamodaranDatasetYear { get; init; }
        public string? DamodaranDatasetRegion { get; init; }

        public List<BenchmarkSuggestionResponse> Suggestions { get; init; } = [];
    }

    /// <summary>One sector's suggestion next to what is on file today.</summary>
    public sealed class BenchmarkSuggestionResponse
    {
        public BenchmarkMetricType MetricType { get; init; }
        public Industry Industry { get; init; }
        public decimal? Value { get; init; }
        public int ComparableCount { get; init; }
        public bool IsDerived { get; init; }
        public List<DerivationStep> Chain { get; init; } = [];
        public List<int> FiscalYears { get; init; } = [];
        public string? Source { get; init; }
        public string? NoSuggestionReason { get; init; }
        public DateTime EffectiveFrom { get; init; }

        /// <summary>Effective value on file today; <c>null</c> when the sector has no multiple yet.</summary>
        public decimal? CurrentValue { get; init; }

        /// <summary>Change from the current value, in percent. <c>null</c> when there is nothing to compare against.</summary>
        public decimal? DeltaPercent { get; init; }

        /// <summary>
        /// A row with this metric, sector, stage and effective date already exists. Surfaced before the
        /// admin clicks, so the duplicate-version conflict is never a surprise 409 afterwards.
        /// </summary>
        public bool CollidesWithExisting { get; init; }
    }
}
