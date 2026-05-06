using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Subscriptions;
using DevStart.Application.Subscriptions;
using DevStart.Domain.StartupMetrics;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMetrics.GetById
{
    internal sealed class GetStartupMetricByIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ISubscriptionChecker subscriptionChecker)
        : IQueryHandler<GetStartupMetricByIdQuery, StartupMetricResponse>
    {
        public async Task<Result<StartupMetricResponse>> Handle(GetStartupMetricByIdQuery query, CancellationToken cancellationToken)
        {
            StartupMetric? startupMetric = await context.StartupMetrics
                .SingleOrDefaultAsync(sm => sm.Id == query.MetricId, cancellationToken);

            if (startupMetric == null)
            {
                return Result.Failure<StartupMetricResponse>(StartupMetricErrors.NotFound(query.MetricId));
            }

            // Premium-MetricType gating: members of the startup always see their own metrics;
            // outside viewers need an active Pro subscription to see Mrr/Mau/MomGrowth/Lvt.
            if (PremiumMetrics.IsPremium(startupMetric.MetricType))
            {
                Guid viewerId = userContext.UserId;
                bool isMember = await context.StartupMembers
                    .AsNoTracking()
                    .AnyAsync(sm => sm.StartupId == startupMetric.StartupId && sm.ProfileId == viewerId, cancellationToken);
                if (!isMember && !await subscriptionChecker.HasActiveProAsync(viewerId, cancellationToken))
                {
                    return Result.Failure<StartupMetricResponse>(SubscriptionErrors.ProRequired);
                }
            }

            StartupMetricResponse startupMetricResponse = new StartupMetricResponse()
            {
                CreatedAt = startupMetric.CreatedAt,
                Id = startupMetric.Id,
                MetricType = startupMetric.MetricType,
                StartupId = startupMetric.StartupId,
                Value = startupMetric.Value,
            };

            return startupMetricResponse;
        }
    }
}
