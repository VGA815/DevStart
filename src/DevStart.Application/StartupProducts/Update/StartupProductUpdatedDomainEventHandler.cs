using DevStart.Application.Abstractions.Data;
using DevStart.Domain.StartupProducts;
using DevStart.SharedKernel;

namespace DevStart.Application.StartupProducts.Update
{
    internal sealed class StartupProductUpdatedDomainEventHandler(ICacheService _cache) : IDomainEventHandler<StartupProductUpdatedDomainEvent>
    {
        public async Task Handle(StartupProductUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            string key = $"v1:startup-products:{domainEvent.StartupProductId}";
            await _cache.RemoveAsync(key);
        }
    }
}
