using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.ServiceOrders;
using DevStart.Domain.ServiceOrders;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Infrastructure.ServiceOrders
{
    internal sealed class ServiceEntitlementChecker(
        IApplicationDbContext context,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider) : IServiceEntitlementChecker
    {
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;

        public async Task<bool> HasAsync(
            Guid userId,
            ServiceType serviceType,
            Guid targetId,
            CancellationToken ct)
        {
            string key = CacheKeys.ServiceEntitlement(userId, (int)serviceType, targetId);
            bool? cached = await cacheService.GetAsync<bool?>(key, ct);
            if (cached.HasValue)
            {
                return cached.Value;
            }

            DateTime utcNow = dateTimeProvider.UtcNow;
            List<DateTime?> windows = await context.ServiceOrders
                .AsNoTracking()
                .Where(o => o.UserId == userId
                         && o.ServiceType == serviceType
                         && o.TargetId == targetId
                         && o.Status == ServiceOrderStatus.Fulfilled
                         && (o.ExpiresAt == null || o.ExpiresAt > utcNow))
                .Select(o => o.ExpiresAt)
                .ToListAsync(ct);

            bool hasAccess = windows.Count > 0;
            bool isPermanent = windows.Exists(w => w is null);

            // Never let a cached "true" outlive the access window: clamp the TTL to what is left, the
            // same guard SubscriptionChecker applies to an expiring subscription.
            TimeSpan ttl = hasAccess && !isPermanent
                ? Min(windows.Max()!.Value - utcNow, CacheTtl)
                : CacheTtl;
            if (ttl > TimeSpan.Zero)
            {
                await cacheService.SetAsync<bool?>(key, hasAccess, ttl, ct);
            }
            return hasAccess;
        }

        public Task InvalidateAsync(Guid userId, CancellationToken ct)
            => cacheService.RemoveByPrefixAsync(CacheKeys.ServiceEntitlementsByUserPrefix(userId), ct);
    }
}
