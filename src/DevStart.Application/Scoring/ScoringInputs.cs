using DevStart.Domain.Startups;

namespace DevStart.Application.Scoring
{
    public sealed record ScoringInputs(
        Guid StartupId,
        StartupStage Stage,
        decimal? Tam,
        decimal? Sam,
        decimal? Som,
        decimal? MarketGrowthRate,
        bool HasPatents,
        int CompetitorsCount,
        IReadOnlyList<MemberInput> Members,
        TractionSignals Traction,
        ProductSignals Product,
        RoadmapSignals Roadmap,
        Industry Industry = Industry.Other,
        decimal? TargetRoundAmount = null,
        bool HasStrategicPartnerships = false);

    /// <summary>
    /// Resolved traction signals. The single home for the dirty-input guard (negative MRR/MAU floored
    /// to 0) and the monthly-recurring → annual rule, so the engine's traction scoring and the
    /// valuation's ARR anchor can never derive these differently.
    /// <see cref="MrrIsProxy"/> marks an MRR that was substituted from the generic Revenue metric
    /// (period undefined) — good enough for the traction score tiers, but never annualized into ARR.
    /// </summary>
    public sealed record TractionSignals(decimal Mrr, decimal Mau, decimal MomGrowth, bool MrrIsProxy = false)
    {
        /// <summary>
        /// Builds the signals, flooring MRR/MAU at 0 (dirty-input guard). MoM growth stays signed —
        /// a negative value legitimately means a declining business.
        /// </summary>
        public static TractionSignals From(decimal? mrr, decimal? mau, decimal? momGrowth, bool mrrIsProxy = false) =>
            new(Math.Max(0m, mrr ?? 0m), Math.Max(0m, mau ?? 0m), momGrowth ?? 0m, mrrIsProxy);

        public static readonly TractionSignals Empty = new(0m, 0m, 0m);

        /// <summary>
        /// Annual recurring revenue (RUB), only from a true monthly-recurring MRR (× 12; floored ≥ 0).
        /// A Revenue-proxied MRR yields 0 — its period is undefined, so annualizing it could overstate
        /// the valuation's revenue anchor up to 12×; the valuation then treats the startup as pre-revenue.
        /// </summary>
        public decimal AnnualRecurringRevenue => MrrIsProxy ? 0m : Mrr * 12m;
    }

    /// <summary>Structural product signals derived from the startup's product description.</summary>
    public sealed record ProductSignals(bool HasArticulatedPositioning)
    {
        public static readonly ProductSignals None = new(false);
    }

    /// <summary>Roadmap planning signals.</summary>
    public sealed record RoadmapSignals(int ItemCount, int DoneCount)
    {
        public static readonly RoadmapSignals None = new(0, 0);
    }
}
