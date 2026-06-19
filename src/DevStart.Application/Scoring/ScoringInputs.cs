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
        RoadmapSignals Roadmap);

    /// <summary>
    /// Resolved traction signals. The single home for the dirty-input guard (negative MRR/MAU floored
    /// to 0) and the monthly-recurring → annual rule, so the engine's traction scoring and the
    /// valuation's ARR anchor can never derive these differently.
    /// </summary>
    public sealed record TractionSignals(decimal Mrr, decimal Mau, decimal MomGrowth)
    {
        /// <summary>
        /// Builds the signals, flooring MRR/MAU at 0 (dirty-input guard). MoM growth stays signed —
        /// a negative value legitimately means a declining business.
        /// </summary>
        public static TractionSignals From(decimal? mrr, decimal? mau, decimal? momGrowth) =>
            new(Math.Max(0m, mrr ?? 0m), Math.Max(0m, mau ?? 0m), momGrowth ?? 0m);

        public static readonly TractionSignals Empty = new(0m, 0m, 0m);

        /// <summary>Annual recurring revenue (RUB). MRR is already floored ≥ 0, so ARR is too.</summary>
        public decimal AnnualRecurringRevenue => Mrr * 12m;
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
