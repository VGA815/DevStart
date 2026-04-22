using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.Notifications;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace DevStart.Infrastructure.Notifications
{
    internal sealed class CentrifugoNotificationSender(
        IHttpClientFactory httpClientFactory,
        ILogger<CentrifugoNotificationSender> logger) : INotificationSender
    {
        public async Task SendAsync(Notification notification, CancellationToken cancellationToken)
        {
            HttpClient client = httpClientFactory.CreateClient("centrifugo");
            var payload = new
            {
                channel = $"notifications:#{notification.UserId}",
                data = new
                {
                    id = notification.Id,
                    type = notification.Type,
                    title = notification.Title,
                    body = notification.Body,
                    createdAt = notification.CreatedAt,
                    referenceId = notification.ReferenceId
                }
            };

            HttpResponseMessage response = await client.PostAsJsonAsync("/api/publish", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "Centrifugo publish failed for notification {NotificationId}: {StatusCode} {Body}",
                    notification.Id,
                    (int)response.StatusCode,
                    responseBody);
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
