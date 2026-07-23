using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.CommunityStandards;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;

namespace DevStart.Application.Startups.Update
{
    internal sealed class StartupUpdatedDomainEventHandler(
        ICacheService cache,
        ICommunityStandardsRefresher communityStandardsRefresher)
        : IDomainEventHandler<StartupUpdatedDomainEvent>
    {
        public async Task Handle(StartupUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await cache.RemoveAsync(CacheKeys.Startup(domainEvent.StartupId), cancellationToken);

            // Description, logo and links are checklist items, so a profile edit can change the badge.
            // The refresher drops the cached checklist itself.
            await communityStandardsRefresher.RefreshAsync(domainEvent.StartupId, cancellationToken);
        }
    }
}
