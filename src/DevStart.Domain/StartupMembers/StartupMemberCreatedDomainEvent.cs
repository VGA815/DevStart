using DevStart.SharedKernel;

namespace DevStart.Domain.StartupMembers
{
    public sealed record StartupMemberCreatedDomainEvent(Guid ProfileId, Guid StartupId) : IDomainEvent;
}
