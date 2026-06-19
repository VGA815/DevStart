using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Subscriptions;
using DevStart.Application.Subscriptions;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMetrics.GetById
{
    internal sealed class GetStartupMetricByIdQueryHandler(
        IApplicationDbContext context,
        IQueryHandler<FetchStartupMetricByIdQuery, StartupMetricResponse> fetchHandler,
        IUserContext userContext,
        ISubscriptionChecker subscriptionChecker)
        : IQueryHandler<GetStartupMetricByIdQuery, StartupMetricResponse>
    {
        public async Task<Result<StartupMetricResponse>> Handle(GetStartupMetricByIdQuery query, CancellationToken cancellationToken)
        {
            // The actual metric read is cached and viewer-independent; the gate below runs on every
            // request, so the cached value can never be returned to an unauthorized viewer.
            Result<StartupMetricResponse> fetched =
                await fetchHandler.Handle(new FetchStartupMetricByIdQuery(query.MetricId), cancellationToken);

            if (fetched.IsFailure)
            {
                return fetched;
            }

            StartupMetricResponse startupMetric = fetched.Value;

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

            return startupMetric;
        }
    }
}
