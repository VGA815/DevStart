using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.Notifications;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;

namespace DevStart.Application.Subscriptions.Activated
{
    internal sealed class SubscriptionActivatedDomainEventHandler(
        INotificationService notificationService,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<SubscriptionActivatedDomainEvent>
    {
        public async Task Handle(SubscriptionActivatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            // Invalidate the active-pro cache so the new state is visible immediately.
            await cacheService.RemoveAsync(
                CacheKeys.SubscriptionActiveByUser(domainEvent.UserId),
                cancellationToken);

            Notification notification = Notification.Create(
                userId: domainEvent.UserId,
                type: NotificationType.SubscriptionActivated,
                title: "Pro subscription activated",
                body: $"Your Pro subscription is active until {domainEvent.ExpiresAt:yyyy-MM-dd}.",
                createdAt: dateTimeProvider.UtcNow,
                referenceId: domainEvent.SubscriptionId);

            await notificationService.PublishAsync(notification, cancellationToken);
        }
    }
}
