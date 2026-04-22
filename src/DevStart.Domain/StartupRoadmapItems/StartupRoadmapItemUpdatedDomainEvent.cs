using DevStart.SharedKernel;

namespace DevStart.Domain.StartupRoadmapItems
{
    public sealed record StartupRoadmapItemUpdatedDomainEvent(Guid ItemId) : IDomainEvent;
}
