using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Profiles;
using DevStart.SharedKernel;

namespace DevStart.Application.Profiles.Update
{
    internal sealed class ProfileUpdatedDomainEventHandler(ICacheService _cache) : IDomainEventHandler<ProfileUpdatedDomainEvent>
    {
        public async Task Handle(ProfileUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            var key = $"v1:profiles:{domainEvent.ProfileId}";
            await _cache.RemoveAsync(key);
        }
    }
}
