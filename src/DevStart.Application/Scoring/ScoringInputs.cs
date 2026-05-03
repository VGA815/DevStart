using DevStart.Domain.StartupMetrics;
using DevStart.Domain.Startups;

namespace DevStart.Application.Scoring
{
    public sealed record ScoringInputs(
        Guid StartupId,
        StartupStage Stage,
        decimal? Tam,
        decimal? MarketGrowthRate,
        bool HasPatents,
        int CompetitorsCount,
        IReadOnlyList<MemberInput> Members,
        IReadOnlyDictionary<MetricType, decimal> LatestMetrics);
}
