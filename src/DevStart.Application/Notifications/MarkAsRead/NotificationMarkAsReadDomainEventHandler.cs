using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Notifications;
using DevStart.SharedKernel;

namespace DevStart.Application.Notifications.MarkAsRead
{
    internal sealed class NotificationMarkAsReadDomainEventHandler(ICacheService cache) : IDomainEventHandler<NotificationMarkAsReadDomainEvent>
    {
        public Task Handle(NotificationMarkAsReadDomainEvent domainEvent, CancellationToken cancellationToken) =>
            cache.RemoveAsync(CacheKeys.Notification(domainEvent.NotificationId), cancellationToken);
    }
}
