using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Notifications.GetByUserId
{
    public sealed record GetNotificationsByUserIdQuery(int Page, int PageSize) : IQuery<List<NotificationResponse>>;
}
