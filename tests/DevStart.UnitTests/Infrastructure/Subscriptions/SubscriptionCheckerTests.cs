using DevStart.Application.Abstractions.Caching;
using DevStart.Domain.Subscriptions;
using DevStart.Infrastructure.Database;
using DevStart.Infrastructure.Subscriptions;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Infrastructure.Subscriptions;

public sealed class SubscriptionCheckerTests
{
    private static readonly DateTime Now = new(2026, 5, 26, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan FullTtl = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly RecordingCacheService _cache = new();
    private readonly SubscriptionChecker _sut;

    public SubscriptionCheckerTests()
    {
        _sut = new SubscriptionChecker(_db, _cache, new FixedDateTimeProvider { UtcNow = Now });
    }

    private async Task SeedActiveAsync(Guid userId, DateTime expiresAt)
    {
        _db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Plan = SubscriptionPlan.Pro,
            Status = SubscriptionStatus.Active,
            StartedAt = Now,
            ExpiresAt = expiresAt,
            CreatedAt = Now,
            UpdatedAt = Now,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task ActiveSubscription_ExpiringSoon_CachesWithRemainingTtlNotFullTtl()
    {
        Guid userId = Guid.NewGuid();
        await SeedActiveAsync(userId, Now.AddMinutes(2));

        bool result = await _sut.HasActiveProAsync(userId, CancellationToken.None);

        result.ShouldBeTrue();
        _cache.LastTtl[CacheKeys.SubscriptionActiveByUser(userId)].ShouldBe(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task ActiveSubscription_FarFromExpiry_CachesWithFullTtl()
    {
        Guid userId = Guid.NewGuid();
        await SeedActiveAsync(userId, Now.AddDays(10));

        bool result = await _sut.HasActiveProAsync(userId, CancellationToken.None);

        result.ShouldBeTrue();
        _cache.LastTtl[CacheKeys.SubscriptionActiveByUser(userId)].ShouldBe(FullTtl);
    }

    [Fact]
    public async Task NoActiveSubscription_ReturnsFalse_CachedWithFullTtl()
    {
        Guid userId = Guid.NewGuid();

        bool result = await _sut.HasActiveProAsync(userId, CancellationToken.None);

        result.ShouldBeFalse();
        _cache.LastTtl[CacheKeys.SubscriptionActiveByUser(userId)].ShouldBe(FullTtl);
    }
}
