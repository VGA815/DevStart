using DevStart.SharedKernel;

namespace DevStart.Domain.Users
{
    public sealed record UserUnbannedDomainEvent(Guid UserId) : IDomainEvent;
}
