using DevStart.Domain.Notifications;

namespace DevStart.Application.Abstractions.Notifications
{
    public interface INotificationSender
    {
        Task SendAsync(Notification notification, CancellationToken cancellationToken);
    }
}
