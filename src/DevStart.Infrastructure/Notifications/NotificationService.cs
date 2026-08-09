using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace DevStart.Infrastructure.Notifications
{
    internal sealed class NotificationService(
        IApplicationDbContext context,
        INotificationSender sender,
        ILogger<NotificationService> logger) : INotificationService
    {
        public async Task PublishAsync(Notification notification, CancellationToken cancellationToken)
        {
            context.Notifications.Add(notification);
            await context.SaveChangesAsync(cancellationToken);

            await SendAsync(notification, cancellationToken);
        }

        public async Task PublishManyAsync(
            IReadOnlyCollection<Notification> notifications,
            CancellationToken cancellationToken)
        {
            if (notifications.Count == 0)
            {
                return;
            }

            context.Notifications.AddRange(notifications);
            await context.SaveChangesAsync(cancellationToken);

            foreach (Notification notification in notifications)
            {
                await SendAsync(notification, cancellationToken);
            }
        }

        // A push failure must not roll back a notification that is already persisted — the recipient
        // still sees it on their next read, they just miss the live nudge.
        private async Task SendAsync(Notification notification, CancellationToken cancellationToken)
        {
            try
            {
                await sender.SendAsync(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to push notification {NotificationId} to Centrifugo", notification.Id);
            }
        }
    }
}
