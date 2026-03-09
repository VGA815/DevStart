namespace DevStart.Application.Abstractions.Notifications
{
    public interface INotificationSender
    {
        Task SendAsync(Guid id, Guid userId, string type, string title, string body, DateTime createdAt, Guid? referenceId = null, CancellationToken cancellationToken = default);
    }
}
