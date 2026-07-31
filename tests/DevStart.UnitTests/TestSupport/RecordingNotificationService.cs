using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.Notifications;

namespace DevStart.UnitTests.TestSupport
{
    /// <summary>Captures published notifications so tests can assert on type, recipient and body.</summary>
    internal sealed class RecordingNotificationService : INotificationService
    {
        public List<Notification> Published { get; } = [];

        public Task PublishAsync(Notification notification, CancellationToken cancellationToken)
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }
    }
}
