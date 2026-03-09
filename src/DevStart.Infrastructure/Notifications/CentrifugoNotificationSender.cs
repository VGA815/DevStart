using DevStart.Application.Abstractions.Notifications;
using System.Net.Http.Json;

namespace DevStart.Infrastructure.Notifications
{
    internal sealed class CentrifugoNotificationSender(IHttpClientFactory httpClientFactory) : INotificationSender
    {
        public async Task SendAsync(Guid id, Guid userId, string type, string title, string body, DateTime createdAt, Guid? referenceId = null, CancellationToken cancellationToken = default)
        {
            var client = httpClientFactory.CreateClient("centrifugo");
            var payload = new
            {
                channel = $"notifications#{userId}",
                data = new
                {
                    id,
                    type,
                    title,
                    body,
                    createdAt,
                    referenceId
                }
            };
            await client.PostAsJsonAsync("/api/publish", payload, cancellationToken);
        }
    }
}
