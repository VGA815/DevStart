using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Scoring;
using DevStart.Domain.StartupMetrics;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Startups.GetScore
{
    internal sealed class GetStartupScoreQueryHandler(
        IApplicationDbContext context,
        IScoringEngine scoringEngine,
        IValuationCalculator valuationCalculator,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetStartupScoreQuery, ScoreResult>
    {
        public async Task<Result<ScoreResult>> Handle(GetStartupScoreQuery query, CancellationToken cancellationToken)
        {
            Startup? startup = await context.Startups
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.Id == query.StartupId, cancellationToken);

            if (startup is null)
            {
                return Result.Failure<ScoreResult>(StartupErrors.NotFound(query.StartupId));
            }

            List<MemberInput> members = await context.StartupMembers
                .AsNoTracking()
                .Where(sm => sm.StartupId == query.StartupId)
                .Select(sm => new MemberInput(
                    sm.ProfileId,
                    sm.Role,
                    sm.Position,
                    sm.YearsOfExperience,
                    sm.HasPriorExit,
                    sm.PreviousStartupsCount))
                .ToListAsync(cancellationToken);

            int competitorsCount = await context.StartupCompetitors
                .AsNoTracking()
                .CountAsync(c => c.StartupId == query.StartupId, cancellationToken);

            // Latest snapshot per metric type for this startup
            List<StartupMetric> metricsRaw = await context.StartupMetrics
                .AsNoTracking()
                .Where(m => m.StartupId == query.StartupId)
                .ToListAsync(cancellationToken);

            Dictionary<MetricType, decimal> latestMetrics = metricsRaw
                .GroupBy(m => m.MetricType)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(m => m.CreatedAt).First().Value);

            ScoringInputs inputs = new(
                StartupId: startup.Id,
                Stage: startup.Stage,
                Tam: startup.Tam,
                MarketGrowthRate: startup.MarketGrowthRate,
                HasPatents: startup.HasPatents,
                CompetitorsCount: competitorsCount,
                Members: members,
                LatestMetrics: latestMetrics);

            DateTime utcNow = dateTimeProvider.UtcNow;
            ScoreResult baseScore = scoringEngine.Compute(inputs, utcNow);
            ValuationRange range = valuationCalculator.ComputeRange(baseScore.TotalScore, startup.Stage);

            return baseScore with
            {
                ValuationLow = range.Low,
                ValuationHigh = range.High,
                MethodsUsed = range.MethodsUsed
            };
        }
    }
}
