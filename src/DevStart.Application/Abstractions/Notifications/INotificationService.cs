using DevStart.Domain.Notifications;

namespace DevStart.Application.Abstractions.Notifications
{
    public interface INotificationService
    {
        Task PublishAsync(Notification notification, CancellationToken cancellationToken);
    }
}
