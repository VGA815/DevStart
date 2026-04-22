using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.StartupRoadmapItems;
using DevStart.SharedKernel;

namespace DevStart.Application.StartupRoadmapItems.Update
{
    internal sealed class StartupRoadmapItemUpdatedDomainEventHandler(ICacheService cache)
        : IDomainEventHandler<StartupRoadmapItemUpdatedDomainEvent>
    {
        public Task Handle(StartupRoadmapItemUpdatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
            cache.RemoveAsync(CacheKeys.StartupRoadmapItem(domainEvent.ItemId), cancellationToken);
    }
}
