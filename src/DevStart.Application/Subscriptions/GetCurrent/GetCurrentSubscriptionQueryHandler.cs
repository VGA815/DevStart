using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Subscriptions.GetCurrent
{
    internal sealed class GetCurrentSubscriptionQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetCurrentSubscriptionQuery, CurrentSubscriptionResponse>
    {
        public async Task<Result<CurrentSubscriptionResponse>> Handle(
            GetCurrentSubscriptionQuery query,
            CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;
            DateTime utcNow = dateTimeProvider.UtcNow;

            Subscription? active = await context.Subscriptions
                .AsNoTracking()
                .Where(s => s.UserId == userId
                         && s.Plan == SubscriptionPlan.Pro
                         && s.Status == SubscriptionStatus.Active
                         && s.ExpiresAt > utcNow)
                .OrderByDescending(s => s.ExpiresAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (active is not null)
            {
                return new CurrentSubscriptionResponse
                {
                    SubscriptionId = active.Id,
                    Plan = active.Plan,
                    Status = active.Status,
                    StartedAt = active.StartedAt,
                    ExpiresAt = active.ExpiresAt,
                    IsActivePro = true,
                };
            }

            Subscription? latest = await context.Subscriptions
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latest is null)
            {
                return new CurrentSubscriptionResponse
                {
                    Plan = SubscriptionPlan.Free,
                    IsActivePro = false,
                };
            }

            return new CurrentSubscriptionResponse
            {
                SubscriptionId = latest.Id,
                Plan = latest.Plan,
                Status = latest.Status,
                StartedAt = latest.StartedAt,
                ExpiresAt = latest.ExpiresAt,
                IsActivePro = false,
            };
        }
    }
}
