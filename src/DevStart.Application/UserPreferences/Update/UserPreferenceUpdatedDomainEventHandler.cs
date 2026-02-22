using DevStart.Application.Abstractions.Data;
using DevStart.Domain.UserPreferences;
using DevStart.SharedKernel;

namespace DevStart.Application.UserPreferences.Update
{
    internal sealed class UserPreferenceUpdatedDomainEventHandler(ICacheService _cache) : IDomainEventHandler<UserPreferenceUpdatedDomainEvent>
    {
        public async Task Handle(UserPreferenceUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            var key = $"v1:user-preferences:{domainEvent.UserPreferenceId}";
            await _cache.RemoveAsync(key);
        }
    }
}
