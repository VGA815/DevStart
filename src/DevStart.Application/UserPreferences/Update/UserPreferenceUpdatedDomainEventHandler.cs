using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.UserPreferences;
using DevStart.SharedKernel;

namespace DevStart.Application.UserPreferences.Update
{
    internal sealed class UserPreferenceUpdatedDomainEventHandler(ICacheService cache) : IDomainEventHandler<UserPreferenceUpdatedDomainEvent>
    {
        public Task Handle(UserPreferenceUpdatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
            cache.RemoveAsync(CacheKeys.UserPreference(domainEvent.UserPreferenceId), cancellationToken);
    }
}
