using DevStart.SharedKernel;

namespace DevStart.Domain.Startups
{
    public sealed record StartupUpdatedDomainEvent(Guid StartupId) : IDomainEvent;
}
