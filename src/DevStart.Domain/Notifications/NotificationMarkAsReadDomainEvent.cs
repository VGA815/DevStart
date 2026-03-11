using DevStart.SharedKernel;

namespace DevStart.Domain.Notifications
{
    public sealed record NotificationMarkAsReadDomainEvent(Guid NotificationId) : IDomainEvent;
}
