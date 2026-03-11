using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Notifications.MarkAsRead
{
    public sealed record MarkNotificationAsReadCommand(Guid NotificationId) : ICommand;
}
