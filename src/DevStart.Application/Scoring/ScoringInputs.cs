using DevStart.Domain.Startups;

namespace DevStart.Application.Scoring
{
    /// <summary>
    /// Everything the engines read. <c>HasRegistryCheckedIp</c> is the odd one out: it feeds the
    /// Product factor's provenance flag and nothing else — no component, no bonus, no valuation input.
    /// See <see cref="ScoreFactorSource.RegistryChecked"/>.
    /// </summary>
    public sealed record ScoringInputs(
        Guid StartupId,
        StartupStage Stage,
        decimal? Tam,
        decimal? Sam,
        decimal? Som,
        decimal? MarketGrowthRate,
        bool HasPatents,
        CompetitorSignals Competitors,
        IReadOnlyList<MemberInput> Members,
        TractionSignals Traction,
        ProductSignals Product,
        RoadmapSignals Roadmap,
        PartnershipSignals Partnerships,
        Industry Industry = Industry.Other,
        decimal? TargetRoundAmount = null,
        bool HasRegistryCheckedIp = false);

    /// <summary>
    /// Resolved traction signals. The single home for the dirty-input guard (negative MRR/MAU floored
    /// to 0) and the monthly-recurring → annual rule, so the engine's traction scoring and the
    /// valuation's ARR anchor can never derive these differently.
    /// <see cref="MrrIsProxy"/> marks an MRR that was substituted from the generic Revenue metric
    /// (period undefined) — good enough for the traction score tiers, but never annualized into ARR.
    /// </summary>
    public sealed record TractionSignals(
        decimal Mrr, decimal Mau, decimal MomGrowth, bool MrrIsProxy = false, bool HasData = false)
    {
        /// <summary>
        /// Builds the signals, flooring MRR/MAU at 0 (dirty-input guard). MoM growth stays signed —
        /// a negative value legitimately means a declining business. <see cref="HasData"/> records
        /// whether any metric was actually on file, so "reported 0" can be told apart from "never
        /// reported" in the score's provenance flag (the score itself is 0 either way).
        /// </summary>
        public static TractionSignals From(decimal? mrr, decimal? mau, decimal? momGrowth, bool mrrIsProxy = false) =>
            new(
                Math.Max(0m, mrr ?? 0m),
                Math.Max(0m, mau ?? 0m),
                momGrowth ?? 0m,
                mrrIsProxy,
                HasData: mrr.HasValue || mau.HasValue || momGrowth.HasValue);

        public static readonly TractionSignals Empty = new(0m, 0m, 0m);

        /// <summary>
        /// Annual recurring revenue (RUB), only from a true monthly-recurring MRR (× 12; floored ≥ 0).
        /// A Revenue-proxied MRR yields 0 — its period is undefined, so annualizing it could overstate
        /// the valuation's revenue anchor up to 12×; the valuation then treats the startup as pre-revenue.
        /// </summary>
        public decimal AnnualRecurringRevenue => MrrIsProxy ? 0m : Mrr * 12m;
    }

    /// <summary>
    /// Competitor-landscape signals. <see cref="TotalCount"/> is carried for transparency only — it is
    /// deliberately NOT a scoring driver, because the startup controls it by adding and deleting cards.
    /// The score is driven by <see cref="WellDocumentedCount"/>: cards that actually carry an analysis
    /// (a website plus at least one of strengths/weaknesses vs us). See docs/scoring-methodology.md.
    /// </summary>
    public sealed record CompetitorSignals(int TotalCount, int WellDocumentedCount)
    {
        public static readonly CompetitorSignals None = new(0, 0);
    }

    /// <summary>
    /// Strategic-partnership signals. Same shape and same reasoning as <see cref="CompetitorSignals"/>:
    /// <see cref="TotalCount"/> travels for transparency only, and the driver is
    /// <see cref="WorkedOutCount"/> — records that actually say what the arrangement is. These replace
    /// the old <c>HasStrategicPartnerships</c> checkbox, which opened a whole Berkus ceiling for one
    /// click (М3 in docs/scoring-inputs-plan.md).
    /// </summary>
    public sealed record PartnershipSignals(int TotalCount, int WorkedOutCount)
    {
        public static readonly PartnershipSignals None = new(0, 0);
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
