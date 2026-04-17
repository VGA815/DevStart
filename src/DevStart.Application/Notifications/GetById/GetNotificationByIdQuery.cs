using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Notifications.GetById
{
    public sealed record GetNotificationByIdQuery(Guid NotificationId) : IQuery<NotificationResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.Notification(NotificationId);

        public TimeSpan Expiration => CacheTtl.Default;
    }
}
