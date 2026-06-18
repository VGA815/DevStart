using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Scoring;
using DevStart.Domain.StartupMetrics;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Startups.GetScore
{
    internal sealed class ComputeStartupScoreQueryHandler(
        IApplicationDbContext context,
        IScoringEngine scoringEngine,
        IValuationCalculator valuationCalculator,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<ComputeStartupScoreQuery, ScoreResult>
    {
        public async Task<Result<ScoreResult>> Handle(ComputeStartupScoreQuery query, CancellationToken cancellationToken)
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

            // ARR anchors the revenue-multiple comparable in the valuation ensemble.
            // Negative/dirty MRR is floored to 0 (pre-revenue). MRR is monthly → ×12 for ARR.
            decimal latestMrr = latestMetrics.TryGetValue(MetricType.Mrr, out decimal mrr) ? mrr : 0m;
            decimal annualRecurringRevenue = Math.Max(0m, latestMrr) * 12m;

            ValuationRange range = valuationCalculator.ComputeRange(
                baseScore.TotalScore, startup.Stage, annualRecurringRevenue);

            return baseScore with
            {
                ValuationLow = range.Low,
                ValuationHigh = range.High,
                MethodsUsed = range.MethodsUsed
            };
        }
    }
}
