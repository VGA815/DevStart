using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Notifications.GetByUserId
{
    public sealed record GetNotificationsByUserIdQuery(bool? IsRead, int Page, int PageSize) : IQuery<List<NotificationResponse>>;
}
