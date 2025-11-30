using DevStart.SharedKernel;

namespace DevStart.Domain.Users
{
    public sealed record UserRegisteredDomainEvent(Guid UserId) : IDomainEvent;
}
