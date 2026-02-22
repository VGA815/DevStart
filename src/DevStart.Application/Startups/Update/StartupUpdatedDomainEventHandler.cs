using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;

namespace DevStart.Application.Startups.Update
{
    internal sealed class StartupUpdatedDomainEventHandler(ICacheService _cache) : IDomainEventHandler<StartupUpdatedDomainEvent>
    {
        public async Task Handle(StartupUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            var key = $"v1:startups:{domainEvent.StartupId}";
            await _cache.RemoveAsync(key);
        }
    }
}
