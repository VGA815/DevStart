using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Subscriptions;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Infrastructure.Subscriptions
{
    internal sealed class SubscriptionChecker(
        IApplicationDbContext context,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider) : ISubscriptionChecker
    {
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public async Task<bool> HasActiveProAsync(Guid userId, CancellationToken ct)
        {
            string key = CacheKeys.SubscriptionActiveByUser(userId);
            bool? cached = await cacheService.GetAsync<bool?>(key, ct);
            if (cached.HasValue)
            {
                return cached.Value;
            }

            DateTime utcNow = dateTimeProvider.UtcNow;
            bool hasActive = await context.Subscriptions
                .AsNoTracking()
                .AnyAsync(
                    s => s.UserId == userId
                      && s.Plan == SubscriptionPlan.Pro
                      && s.Status == SubscriptionStatus.Active
                      && s.ExpiresAt > utcNow,
                    ct);

            await cacheService.SetAsync<bool?>(key, hasActive, CacheTtl, ct);
            return hasActive;
        }
    }
}
