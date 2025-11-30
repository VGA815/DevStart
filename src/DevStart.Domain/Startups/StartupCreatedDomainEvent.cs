using DevStart.SharedKernel;

namespace DevStart.Domain.Startups
{
    public sealed record StartupCreatedDomainEvent(Guid StartupId) : IDomainEvent;
}
