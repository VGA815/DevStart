using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.TwoFactor;
using DevStart.SharedKernel;

namespace DevStart.Application.Users.TwoFactor
{
    /// <summary>
    /// Drops the cached user projection so <c>twoFactorEnabled</c> is reflected immediately
    /// (same pattern as the ban/unban cache eviction).
    /// </summary>
    internal sealed class TwoFactorEnabledDomainEventHandler(ICacheService cacheService)
        : IDomainEventHandler<TwoFactorEnabledDomainEvent>
    {
        public async Task Handle(TwoFactorEnabledDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await cacheService.RemoveAsync(CacheKeys.User(domainEvent.UserId), cancellationToken);
        }
    }

    internal sealed class TwoFactorDisabledDomainEventHandler(ICacheService cacheService)
        : IDomainEventHandler<TwoFactorDisabledDomainEvent>
    {
        public async Task Handle(TwoFactorDisabledDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await cacheService.RemoveAsync(CacheKeys.User(domainEvent.UserId), cancellationToken);
        }
    }
}
