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
