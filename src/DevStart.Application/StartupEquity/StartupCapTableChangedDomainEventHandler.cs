using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.StartupEquity;
using DevStart.SharedKernel;

namespace DevStart.Application.StartupEquity
{
    /// <summary>Invalidates the cached startup score when the founding cap table changes, since team
    /// composition and equity feed into scoring/valuation.</summary>
    internal sealed class StartupCapTableChangedDomainEventHandler(ICacheService cacheService)
        : IDomainEventHandler<StartupCapTableChangedDomainEvent>
    {
        public Task Handle(StartupCapTableChangedDomainEvent domainEvent, CancellationToken cancellationToken)
            => cacheService.RemoveAsync(CacheKeys.StartupScore(domainEvent.StartupId), cancellationToken);
    }
}
