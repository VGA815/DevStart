using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;

namespace DevStart.Application.Scoring.Benchmarks
{
    /// <summary>
    /// The knobs of a derivation run. They arrive on the request and travel into the resulting
    /// <see cref="BenchmarkSuggestion.Source"/> — deliberately not into <c>appsettings.json</c>.
    ///
    /// The precedent is in this repository: <c>MethodologyVersion</c> lived both as a code default and
    /// as a config value, config won, and a batch of snapshots went out under the wrong label. A
    /// parameter that changes what a number *means* and is stored apart from that number will
    /// eventually disagree with it. Here the parameter physically rides along with the result, so there
    /// is nowhere for the two to drift apart.
    /// </summary>
    public sealed record BenchmarkDerivationParameters(
        int MinComparables,
        decimal CountryDiscount,
        decimal IlliquidityAndSizeDiscount,
        string DatasetRegion,
        DateTime AsOf)
    {
        /// <summary>
        /// Shown as the form's defaults and used when the caller sends nothing. Emerging Markets is the
        /// standing choice of slice: closer to the Russian risk profile than Global, which keeps the
        /// country discount small enough to be an adjustment rather than the whole answer.
        /// </summary>
        public static BenchmarkDerivationParameters Defaults(DateTime asOf) => new(
            MinComparables: 3,
            CountryDiscount: 0.60m,
            IlliquidityAndSizeDiscount: 0.70m,
            DatasetRegion: "Emerging Markets",
            AsOf: asOf);
    }

    /// <summary>One Damodaran bucket as staged.</summary>
    public sealed record DamodaranBucketInput(string ExternalKey, decimal EvSales, int DatasetYear, string? Region);

    /// <summary>
    /// One Russian comparable with both halves of its multiple present. An issuer missing either half
    /// never reaches the engine — a comparable is a pair, not a market cap.
    /// </summary>
    public sealed record ComparableInput(
        string Ticker,
        Industry Industry,
        decimal MarketCap,
        decimal Revenue,
        int? FiscalYear,
        bool RevenueIsManual);

    /// <summary>Everything the derivation reads. No connections, no clock, no configuration.</summary>
    public sealed record BenchmarkDerivationInputs(
        IReadOnlyList<DamodaranBucketInput> Buckets,
        IReadOnlyDictionary<string, Industry?> BucketMappings,
        IReadOnlyList<ComparableInput> Comparables);

    /// <summary>One link of the calculation, with the value it produced.</summary>
    public sealed record DerivationStep(string Label, decimal? Value, string Detail);

    /// <summary>
    /// What the engine offers for one sector. A suggestion with <see cref="Value"/> <c>null</c> is a
    /// first-class answer — "there is nothing to suggest" — and never a zero or an invented figure, the
    /// same policy the valuation engine follows since its hardcoded fallback was removed.
    /// </summary>
    public sealed class BenchmarkSuggestion
    {
        public BenchmarkMetricType MetricType { get; init; }

        public Industry Industry { get; init; }

        public decimal? Value { get; init; }

        /// <summary>
        /// How many Russian comparables back the figure. The single strongest trust signal in the
        /// output: three comparables and one comparable are not the same claim.
        /// </summary>
        public int ComparableCount { get; init; }

        /// <summary>
        /// <c>true</c> when the country coefficient was computed from those comparables, <c>false</c>
        /// when it came in as a parameter. Keeps a calculation and an assumption visibly apart.
        /// </summary>
        public bool IsDerived { get; init; }

        public IReadOnlyList<DerivationStep> Chain { get; init; } = [];

        /// <summary>Fiscal years of the revenue used. Plural because issuers file at different times.</summary>
        public IReadOnlyList<int> FiscalYears { get; init; } = [];

        /// <summary>Ready to paste into the add form; <c>null</c> when there is no suggestion.</summary>
        public string? Source { get; init; }

        /// <summary>Why there is nothing to suggest; <c>null</c> when there is.</summary>
        public string? NoSuggestionReason { get; init; }

        /// <summary>
        /// The date the row would take effect: the start of the quarter the data describes, not the day
        /// an admin clicked. Editable in the form.
        /// </summary>
        public DateTime EffectiveFrom { get; init; }
    }
}
