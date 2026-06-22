using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DevStart.Application.Admin.Subscriptions.ExtendSubscription
{
    internal sealed class ExtendSubscriptionCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<ExtendSubscriptionCommand>
    {
        public async Task<Result> Handle(ExtendSubscriptionCommand command, CancellationToken cancellationToken)
        {
            Subscription? subscription = await context.Subscriptions
                .SingleOrDefaultAsync(s => s.Id == command.SubscriptionId, cancellationToken);
            if (subscription is null)
            {
                return Result.Failure(SubscriptionErrors.NotFound(command.SubscriptionId));
            }

            DateTime now = dateTimeProvider.UtcNow;
            Result extended = subscription.Extend(command.AdditionalDays, now);
            if (extended.IsFailure)
            {
                return extended;
            }

            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.ExtendSubscription,
                AdminTargetType.Subscription,
                subscription.Id,
                command.Reason,
                now,
                JsonSerializer.Serialize(new { command.AdditionalDays, newExpiresAt = subscription.ExpiresAt })));

            await context.SaveChangesAsync(cancellationToken);

            // The active-pro cache clamps its TTL to the old expiry; drop it so the new term is seen at once.
            await cacheService.RemoveAsync(
                CacheKeys.SubscriptionActiveByUser(subscription.UserId), cancellationToken);

            return Result.Success();
        }
    }
}
