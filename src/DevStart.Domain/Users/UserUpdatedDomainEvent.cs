using DevStart.SharedKernel;

namespace DevStart.Domain.Users
{
    public sealed record UserUpdatedDomainEvent(Guid UserId) : IDomainEvent;
}
