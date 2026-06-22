using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Subscriptions;
using DevStart.Domain.Admin;
using DevStart.Domain.Subscriptions;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DevStart.Application.Admin.Subscriptions.GrantSubscription
{
    internal sealed class GrantSubscriptionCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IOptions<PlansOptions> plansOptions,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<GrantSubscriptionCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(GrantSubscriptionCommand command, CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;

            bool userExists = await context.Users.AnyAsync(u => u.Id == command.UserId, cancellationToken);
            if (!userExists)
            {
                return Result.Failure<Guid>(UserErrors.NotFound(command.UserId));
            }

            bool hasActive = await context.Subscriptions.AnyAsync(
                s => s.UserId == command.UserId
                  && s.Plan == SubscriptionPlan.Pro
                  && s.Status == SubscriptionStatus.Active
                  && s.ExpiresAt > now,
                cancellationToken);
            if (hasActive)
            {
                // Use Extend for an already-active subscription.
                return Result.Failure<Guid>(SubscriptionErrors.AlreadyActive);
            }

            int durationDays = command.DurationDays ?? plansOptions.Value.Pro.DurationDays;

            Subscription subscription = Subscription.CreatePending(
                command.UserId, SubscriptionPlan.Pro, now, SubscriptionSource.AdminGrant);

            // Activate raises SubscriptionActivatedDomainEvent → drops the active-pro cache + notifies the user.
            Result activated = subscription.Activate(now, durationDays);
            if (activated.IsFailure)
            {
                return Result.Failure<Guid>(activated.Error);
            }

            context.Subscriptions.Add(subscription);
            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.GrantSubscription,
                AdminTargetType.Subscription,
                subscription.Id,
                command.Reason,
                now,
                JsonSerializer.Serialize(new { command.UserId, durationDays })));

            await context.SaveChangesAsync(cancellationToken);
            return subscription.Id;
        }
    }
}
