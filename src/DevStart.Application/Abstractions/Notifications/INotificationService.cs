using DevStart.Domain.Notifications;

namespace DevStart.Application.Abstractions.Notifications
{
    public interface INotificationService
    {
        Task PublishAsync(Notification notification, CancellationToken cancellationToken);

        /// <summary>
        /// Persists a fan-out batch in a single transaction, then pushes each notification.
        /// Calling <see cref="PublishAsync"/> in a loop costs one SaveChanges — and therefore one
        /// pass over the whole change tracker — per recipient, which is why every fan-out site
        /// uses this instead.
        /// </summary>
        Task PublishManyAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken);
    }
}
