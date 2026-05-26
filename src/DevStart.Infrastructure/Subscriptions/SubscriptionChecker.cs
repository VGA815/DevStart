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

        private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;

        public async Task<bool> HasActiveProAsync(Guid userId, CancellationToken ct)
        {
            string key = CacheKeys.SubscriptionActiveByUser(userId);
            bool? cached = await cacheService.GetAsync<bool?>(key, ct);
            if (cached.HasValue)
            {
                return cached.Value;
            }

            DateTime utcNow = dateTimeProvider.UtcNow;
            DateTime? activeUntil = await context.Subscriptions
                .AsNoTracking()
                .Where(s => s.UserId == userId
                         && s.Plan == SubscriptionPlan.Pro
                         && s.Status == SubscriptionStatus.Active
                         && s.ExpiresAt > utcNow)
                .MaxAsync(s => (DateTime?)s.ExpiresAt, ct);

            bool hasActive = activeUntil.HasValue;

            // Never let a cached "true" outlive the subscription: clamp the TTL to the remaining term so
            // access is re-checked against the DB the moment it ends, not up to CacheTtl later.
            TimeSpan ttl = hasActive
                ? Min(activeUntil!.Value - utcNow, CacheTtl)
                : CacheTtl;
            if (ttl > TimeSpan.Zero)
            {
                await cacheService.SetAsync<bool?>(key, hasActive, ttl, ct);
            }
            return hasActive;
        }
    }
}
