using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Notifications.GetById
{
    public sealed record GetNotificationByIdQuery(Guid NotificationId) : IQuery<NotificationResponse>, ICacheableQuery
    {
        public string CacheKey => $"v1:notifications:{NotificationId}";

        public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
    }
}
