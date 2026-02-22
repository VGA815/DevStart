using DevStart.SharedKernel;

namespace DevStart.Domain.StartupProducts
{
    public sealed record StartupProductUpdatedDomainEvent(Guid StartupProductId) : IDomainEvent;
}
