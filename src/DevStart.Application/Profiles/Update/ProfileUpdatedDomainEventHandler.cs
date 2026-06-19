using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Profiles;
using DevStart.SharedKernel;

namespace DevStart.Application.Profiles.Update
{
    internal sealed class ProfileUpdatedDomainEventHandler(ICacheService cache) : IDomainEventHandler<ProfileUpdatedDomainEvent>
    {
        public Task Handle(ProfileUpdatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
            Task.WhenAll(
                cache.RemoveAsync(CacheKeys.Profile(domainEvent.ProfileId), cancellationToken),
                // The aggregated user overview embeds the profile, so it must be invalidated too.
                cache.RemoveAsync(CacheKeys.UserOverview(domainEvent.ProfileId), cancellationToken));
    }
}
