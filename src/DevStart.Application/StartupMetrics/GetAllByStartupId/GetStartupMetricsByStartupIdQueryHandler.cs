using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Subscriptions;
using DevStart.Application.Subscriptions;
using DevStart.Domain.StartupMetrics;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMetrics.GetAllByStartupId
{
    internal sealed class GetStartupMetricsByStartupIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ISubscriptionChecker subscriptionChecker)
        : IQueryHandler<GetStartupMetricsByStartupIdQuery, List<StartupMetricResponse>>
    {
        public async Task<Result<List<StartupMetricResponse>>> Handle(GetStartupMetricsByStartupIdQuery query, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(sm => sm.Id == query.StartupId, cancellationToken))
            {
                return Result.Failure<List<StartupMetricResponse>>(StartupErrors.NotFound(query.StartupId));
            }

            // Premium gating: viewers who are members of the startup see all metrics;
            // outside viewers without Pro have premium MetricType rows filtered out.
            Guid viewerId = userContext.UserId;
            bool isMember = await context.StartupMembers
                .AsNoTracking()
                .AnyAsync(sm => sm.StartupId == query.StartupId && sm.ProfileId == viewerId, cancellationToken);
            bool canSeePremium = isMember
                || await subscriptionChecker.HasActiveProAsync(viewerId, cancellationToken);

            IQueryable<StartupMetric> metricsQuery = context.StartupMetrics
                .Where(sm => sm.StartupId == query.StartupId);

            // Filter premium MetricType rows in the query (not after fetching) so they are never loaded
            // for viewers without access.
            if (!canSeePremium)
            {
                MetricType[] premiumTypes = [.. PremiumMetrics.Types];
                metricsQuery = metricsQuery.Where(sm => !premiumTypes.Contains(sm.MetricType));
            }

            List<StartupMetricResponse> startupMetricResponses = await metricsQuery
                .Select(sm => new StartupMetricResponse
                {
                    CreatedAt = sm.CreatedAt,
                    Id = sm.Id,
                    MetricType = sm.MetricType,
                    StartupId = sm.StartupId,
                    Value = sm.Value,
                })
                .ToListAsync(cancellationToken);

            return startupMetricResponses;
        }
    }
}
