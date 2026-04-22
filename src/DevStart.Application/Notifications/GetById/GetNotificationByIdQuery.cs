using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Notifications.GetById
{
    public sealed record GetNotificationByIdQuery(Guid NotificationId) : IQuery<NotificationResponse>;
}
