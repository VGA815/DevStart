using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Profiles;
using DevStart.SharedKernel;

namespace DevStart.Application.Profiles.Update
{
    internal sealed class ProfileUpdatedDomainEventHandler(ICacheService cache) : IDomainEventHandler<ProfileUpdatedDomainEvent>
    {
        public Task Handle(ProfileUpdatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
            cache.RemoveAsync(CacheKeys.Profile(domainEvent.ProfileId), cancellationToken);
    }
}
