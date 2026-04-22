using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Notifications.GetUnreadCount
{
    public sealed record GetUnreadCountQuery : IQuery<int>;
}
