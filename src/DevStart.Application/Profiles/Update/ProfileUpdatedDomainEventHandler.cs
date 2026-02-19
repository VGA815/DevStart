using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Profiles;
using DevStart.SharedKernel;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevStart.Application.Profiles.Update
{
    internal sealed class ProfileUpdatedDomainEventHandler(ICacheService _cache) : IDomainEventHandler<ProfileUpdatedDomainEvent>
    {
        public async Task Handle(ProfileUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            var key = $"v1:user:{domainEvent.ProfileId}";
            await _cache.RemoveAsync(key);
        }
    }
}
