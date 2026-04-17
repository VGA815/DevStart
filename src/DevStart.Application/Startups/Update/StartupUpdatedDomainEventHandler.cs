using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;

namespace DevStart.Application.Startups.Update
{
    internal sealed class StartupUpdatedDomainEventHandler(ICacheService cache) : IDomainEventHandler<StartupUpdatedDomainEvent>
    {
        public Task Handle(StartupUpdatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
            cache.RemoveAsync(CacheKeys.Startup(domainEvent.StartupId), cancellationToken);
    }
}
