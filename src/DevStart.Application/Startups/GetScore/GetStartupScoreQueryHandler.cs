using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.ServiceOrders;
using DevStart.Application.Abstractions.Subscriptions;
using DevStart.Application.Scoring;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.Startups;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Startups.GetScore
{
    internal sealed class GetStartupScoreQueryHandler(
        IApplicationDbContext context,
        IQueryHandler<ComputeStartupScoreQuery, ScoreResult> computeScoreHandler,
        IUserContext userContext,
        ISubscriptionChecker subscriptionChecker,
        IServiceEntitlementChecker entitlementChecker)
        : IQueryHandler<GetStartupScoreQuery, ScoreResult>
    {
        public async Task<Result<ScoreResult>> Handle(GetStartupScoreQuery query, CancellationToken cancellationToken)
        {
            bool startupExists = await context.Startups
                .AsNoTracking()
                .AnyAsync(s => s.Id == query.StartupId, cancellationToken);

            if (!startupExists)
            {
                return Result.Failure<ScoreResult>(StartupErrors.NotFound(query.StartupId));
            }

            // Pro gating: members of this startup can always see the score; outside viewers need Pro, or
            // a paid one-time scoring-report order for this specific startup (SC-49).
            // This gate runs on every request — the cached computation lives in ComputeStartupScoreQuery,
            // so a warm cache can never let an unauthorized viewer bypass the paywall.
            Guid viewerId = userContext.UserId;
            bool isMember = await context.StartupMembers
                .AsNoTracking()
                .AnyAsync(sm => sm.StartupId == query.StartupId && sm.ProfileId == viewerId, cancellationToken);
            if (!isMember
                && !await subscriptionChecker.HasActiveProAsync(viewerId, cancellationToken)
                && !await entitlementChecker.HasAsync(
                        viewerId, ServiceType.ScoringReport, query.StartupId, cancellationToken))
            {
                return Result.Failure<ScoreResult>(SubscriptionErrors.ProRequired);
            }

            return await computeScoreHandler.Handle(new ComputeStartupScoreQuery(query.StartupId), cancellationToken);
        }
    }
}
