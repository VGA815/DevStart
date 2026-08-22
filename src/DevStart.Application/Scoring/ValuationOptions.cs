using DevStart.Domain.Startups;

namespace DevStart.Application.Scoring
{
    /// <summary>
    /// Tunable constants for the valuation ensemble (all amounts RUB). Bound from the "Valuation"
    /// configuration section; every value has a proposed default so the engine works without explicit
    /// config. Bumping <see cref="MethodologyVersion"/> (or any constant) is recorded on each result and
    /// snapshot for transparency/backtesting.
    ///
    /// Stage/sector lookups are keyed by the <see cref="StartupStage"/> / <see cref="Industry"/> enums
    /// (the configuration binder parses the string keys in appsettings into the enum values), so a
    /// renamed enum member fails to bind loudly instead of silently falling through to a default.
    /// </summary>
    public sealed class ValuationOptions
    {
        public const string SectionName = "Valuation";

        // Covers both methodologies (scoring and valuation) — they share this one version string, which
        // travels on every result and snapshot.
        // v6: withholding an input is never the better move, in the valuation ensemble too. The VC
        // method drops out until a target round is declared (an empty field used to skip the
        // subtraction and hand back the largest pre-money), and the fifth Berkus factor is graded by
        // the count of worked-out partnership records instead of a one-click checkbox.
        // v5: the competition scoring factor is driven by the quality of the startup's competitor
        // analysis plus an external sector-intensity benchmark, never by the number of cards; a factor
        // with no data drops out and the weights renormalize (scoring and Scorecard alike).
        // v4: the valuation's ARR anchor is derived only from a true Mrr metric — a Revenue-proxied
        // MRR no longer annualizes into ARR (its period is undefined), so Comparable/VC inputs changed.
        public string MethodologyVersion { get; set; } = "v6-2026.08-no-gain-from-withholding";

        /// <summary>Half-width of the band drawn around the weighted point estimate (e.g. 0.25 = ±25%).</summary>
        public decimal RangeBand { get; set; } = 0.25m;

        // Base ensemble weights per method; renormalized over the stage-applicable subset.
        public decimal BerkusWeight { get; set; } = 1m;
        public decimal ScorecardWeight { get; set; } = 1m;
        public decimal VcWeight { get; set; } = 1m;
        public decimal ComparableWeight { get; set; } = 1m;

        public BerkusOptions Berkus { get; set; } = new();
        public ScorecardOptions Scorecard { get; set; } = new();
        public VcMethodOptions Vc { get; set; } = new();
    }

    /// <summary>Berkus: five factors, each capped by a RUB ceiling; a 0..1 signal scales each ceiling.</summary>
    public sealed class BerkusOptions
    {
        public decimal IdeaCeiling { get; set; } = 45_000_000m;
        public decimal PrototypeCeiling { get; set; } = 45_000_000m;
        public decimal TeamCeiling { get; set; } = 45_000_000m;
        public decimal PartnershipsCeiling { get; set; } = 45_000_000m;
        public decimal TractionCeiling { get; set; } = 45_000_000m;
    }

    /// <summary>
    /// Scorecard: a stage/sector median multiplied by 7 weighted 0.5–1.5 factor multipliers. The median
    /// itself is data, not config — it comes from <c>IValuationBenchmarkProvider</c> (the
    /// <c>valuation_benchmark</c> table); only the factor weights and the multiplier band live here.
    /// </summary>
    public sealed class ScorecardOptions
    {
        // Bill-Payne factor weights (sum ≈ 1.0). "Sales" is proxied by the traction sub-score.
        public decimal TeamWeight { get; set; } = 0.30m;
        public decimal MarketWeight { get; set; } = 0.25m;
        public decimal ProductWeight { get; set; } = 0.15m;
        public decimal CompetitionWeight { get; set; } = 0.10m;
        public decimal SalesWeight { get; set; } = 0.10m;
        public decimal FinancingWeight { get; set; } = 0.05m;
        public decimal OtherWeight { get; set; } = 0.05m;

        public decimal MultiplierFloor { get; set; } = 0.5m;
        public decimal MultiplierCeiling { get; set; } = 1.5m;
    }

    /// <summary>VC Method: reverse from a projected exit (TV = exit revenue × multiple; post = TV / (1+IRR)^n).</summary>
    public sealed class VcMethodOptions
    {
        /// <summary>EV/Revenue exit multiple by sector.</summary>
        public Dictionary<Industry, decimal> SectorExitMultiples { get; set; } = new()
        {
            [Industry.Saas] = 6m,
            [Industry.Fintech] = 5m,
            [Industry.Ai] = 8m,
            [Industry.Ecommerce] = 3m,
            [Industry.Marketplace] = 4m,
            [Industry.Hardware] = 3m,
            [Industry.Biotech] = 5m,
            [Industry.Edtech] = 4m,
            [Industry.Other] = 4m,
        };
        public decimal DefaultExitMultiple { get; set; } = 4m;

        /// <summary>Required IRR by stage (decimal fraction, e.g. 0.40 = 40%).</summary>
        public Dictionary<StartupStage, decimal> StageIrr { get; set; } = new()
        {
            [StartupStage.Mvp] = 0.55m,
            [StartupStage.Seed] = 0.50m,
            [StartupStage.SeriesA] = 0.40m,
        };
        public decimal DefaultIrr { get; set; } = 0.50m;

        public int HorizonYears { get; set; } = 5;

        /// <summary>Multiple applied to current ARR to project revenue at exit.</summary>
        public decimal ExitRevenueGrowthMultiple { get; set; } = 10m;

        /// <summary>Assumed exit revenue by stage when the startup is pre-revenue (RUB).</summary>
        public Dictionary<StartupStage, decimal> PreRevenueExitRevenue { get; set; } = new()
        {
            [StartupStage.Mvp] = 100_000_000m,
            [StartupStage.Seed] = 250_000_000m,
            [StartupStage.SeriesA] = 500_000_000m,
        };
        public decimal DefaultPreRevenueExitRevenue { get; set; } = 250_000_000m;
    }
}
