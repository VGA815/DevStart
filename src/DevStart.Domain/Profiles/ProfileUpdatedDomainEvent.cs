using DevStart.SharedKernel;

namespace DevStart.Domain.Profiles
{
    public sealed record ProfileUpdatedDomainEvent(Guid ProfileId) : IDomainEvent; 
}
