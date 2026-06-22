using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Users;
using DevStart.SharedKernel;

namespace DevStart.Application.Users.Banned
{
    /// <summary>
    /// Drops the cached user/overview projections so the banned state is reflected immediately.
    /// </summary>
    internal sealed class UserBannedDomainEventHandler(ICacheService cacheService)
        : IDomainEventHandler<UserBannedDomainEvent>
    {
        public async Task Handle(UserBannedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await cacheService.RemoveAsync(CacheKeys.User(domainEvent.UserId), cancellationToken);
            await cacheService.RemoveAsync(CacheKeys.UserOverview(domainEvent.UserId), cancellationToken);
        }
    }
}
