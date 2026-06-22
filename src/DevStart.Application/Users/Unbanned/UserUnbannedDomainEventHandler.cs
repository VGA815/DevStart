using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Users;
using DevStart.SharedKernel;

namespace DevStart.Application.Users.Unbanned
{
    internal sealed class UserUnbannedDomainEventHandler(ICacheService cacheService)
        : IDomainEventHandler<UserUnbannedDomainEvent>
    {
        public async Task Handle(UserUnbannedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await cacheService.RemoveAsync(CacheKeys.User(domainEvent.UserId), cancellationToken);
            await cacheService.RemoveAsync(CacheKeys.UserOverview(domainEvent.UserId), cancellationToken);
        }
    }
}
