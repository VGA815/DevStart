using System.Collections.Concurrent;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.Notifications;

namespace DevStart.IntegrationTests.Fakes
{
    /// <summary>In-memory <see cref="INotificationSender"/> replacing Centrifugo. Records the notifications
    /// that would have been pushed over the websocket so tests can assert on them without a real broker.</summary>
    internal sealed class RecordingNotificationSender : INotificationSender
    {
        public ConcurrentQueue<Notification> Sent { get; } = new();

        public Task SendAsync(Notification notification, CancellationToken cancellationToken)
        {
            Sent.Enqueue(notification);
            return Task.CompletedTask;
        }
    }
}
