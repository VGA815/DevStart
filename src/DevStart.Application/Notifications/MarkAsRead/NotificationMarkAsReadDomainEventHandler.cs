using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Notifications;
using DevStart.SharedKernel;

namespace DevStart.Application.Notifications.MarkAsRead
{
    internal sealed class NotificationMarkAsReadDomainEventHandler(ICacheService cacheService) : IDomainEventHandler<NotificationMarkAsReadDomainEvent>
    {
        public async Task Handle(NotificationMarkAsReadDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            var key = $"v1:notifications:{domainEvent.NotificationId}";
            await cacheService.RemoveAsync(key);
        }
    }
}