using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.StartupProducts;
using DevStart.SharedKernel;

namespace DevStart.Application.StartupProducts.Update
{
    internal sealed class StartupProductUpdatedDomainEventHandler(ICacheService cache) : IDomainEventHandler<StartupProductUpdatedDomainEvent>
    {
        public Task Handle(StartupProductUpdatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
            cache.RemoveAsync(CacheKeys.StartupProduct(domainEvent.StartupProductId), cancellationToken);
    }
}
