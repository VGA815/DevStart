using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.Notifications;
using DevStart.Domain.Payments;
using DevStart.SharedKernel;

namespace DevStart.Application.Payments.Refunded
{
    internal sealed class PaymentRefundedDomainEventHandler(
        INotificationService notificationService,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<PaymentRefundedDomainEvent>
    {
        public async Task Handle(PaymentRefundedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            // The subscription is cancelled on a full refund; drop the active-pro cache immediately.
            await cacheService.RemoveAsync(
                CacheKeys.SubscriptionActiveByUser(domainEvent.UserId),
                cancellationToken);

            Notification notification = Notification.Create(
                userId: domainEvent.UserId,
                type: NotificationType.PaymentRefunded,
                title: "Payment refunded",
                body: "Your payment was refunded and the Pro subscription has been deactivated.",
                createdAt: dateTimeProvider.UtcNow,
                referenceId: domainEvent.SubscriptionId);

            await notificationService.PublishAsync(notification, cancellationToken);
        }
    }
}
