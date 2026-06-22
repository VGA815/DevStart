using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Subscriptions.RevokeSubscription
{
    internal sealed class RevokeSubscriptionCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<RevokeSubscriptionCommand>
    {
        public async Task<Result> Handle(RevokeSubscriptionCommand command, CancellationToken cancellationToken)
        {
            Subscription? subscription = await context.Subscriptions
                .SingleOrDefaultAsync(s => s.Id == command.SubscriptionId, cancellationToken);
            if (subscription is null)
            {
                return Result.Failure(SubscriptionErrors.NotFound(command.SubscriptionId));
            }

            DateTime now = dateTimeProvider.UtcNow;
            subscription.MarkCancelled(now);

            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.RevokeSubscription,
                AdminTargetType.Subscription,
                subscription.Id,
                command.Reason,
                now));

            await context.SaveChangesAsync(cancellationToken);

            // Revoke Pro access immediately.
            await cacheService.RemoveAsync(
                CacheKeys.SubscriptionActiveByUser(subscription.UserId), cancellationToken);

            return Result.Success();
        }
    }
}
