using DevStart.Application.Abstractions.Caching;
using DevStart.Domain.ServiceOrders;
using DevStart.Infrastructure.Database;
using DevStart.Infrastructure.ServiceOrders;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Infrastructure.ServiceOrders;

public sealed class ServiceEntitlementCheckerTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly RecordingCacheService _cache = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _targetId = Guid.NewGuid();

    private ServiceEntitlementChecker CreateSut(DateTime? now = null)
        => new(_db, _cache, new FixedDateTimeProvider { UtcNow = now ?? Now });

    private async Task SeedAsync(
        ServiceOrderStatus status,
        int accessDays,
        DateTime? fulfilledAt = null,
        ServiceType serviceType = ServiceType.ScoringReport,
        Guid? targetId = null)
    {
        DateTime at = fulfilledAt ?? Now;
        ServiceOrder order = ServiceOrder.CreatePending(
            _userId, serviceType, targetId ?? _targetId, 490m, "RUB", at);

        if (status is not ServiceOrderStatus.Pending)
        {
            order.MarkPaid(at);
        }
        if (status is ServiceOrderStatus.Fulfilled or ServiceOrderStatus.Refunded or ServiceOrderStatus.Cancelled)
        {
            order.MarkFulfilled(at, accessDays);
        }
        if (status == ServiceOrderStatus.Refunded)
        {
            order.MarkRefunded(at);
        }
        if (status == ServiceOrderStatus.Cancelled)
        {
            order.MarkCancelled(at);
        }

        _db.ServiceOrders.Add(order);
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Fulfilled_order_within_its_window_grants_access()
    {
        await SeedAsync(ServiceOrderStatus.Fulfilled, accessDays: 30);

        bool has = await CreateSut().HasAsync(_userId, ServiceType.ScoringReport, _targetId, CancellationToken.None);

        has.ShouldBeTrue();
    }

    [Fact]
    public async Task Expired_order_does_not_grant_access()
    {
        await SeedAsync(ServiceOrderStatus.Fulfilled, accessDays: 30, fulfilledAt: Now.AddDays(-40));

        bool has = await CreateSut().HasAsync(_userId, ServiceType.ScoringReport, _targetId, CancellationToken.None);

        has.ShouldBeFalse();
    }

    [Fact]
    public async Task Paid_but_unfulfilled_order_does_not_grant_access()
    {
        await SeedAsync(ServiceOrderStatus.Paid, accessDays: 30);

        bool has = await CreateSut().HasAsync(_userId, ServiceType.ScoringReport, _targetId, CancellationToken.None);

        has.ShouldBeFalse();
    }

    [Fact]
    public async Task Refunded_order_does_not_grant_access()
    {
        await SeedAsync(ServiceOrderStatus.Refunded, accessDays: 30);

        bool has = await CreateSut().HasAsync(_userId, ServiceType.ScoringReport, _targetId, CancellationToken.None);

        has.ShouldBeFalse();
    }

    [Fact]
    public async Task Cancelled_order_does_not_grant_access()
    {
        await SeedAsync(ServiceOrderStatus.Cancelled, accessDays: 30);

        bool has = await CreateSut().HasAsync(_userId, ServiceType.ScoringReport, _targetId, CancellationToken.None);

        has.ShouldBeFalse();
    }

    [Fact]
    public async Task Access_is_scoped_to_the_target_it_was_bought_for()
    {
        await SeedAsync(ServiceOrderStatus.Fulfilled, accessDays: 30);

        bool otherTarget = await CreateSut()
            .HasAsync(_userId, ServiceType.ScoringReport, Guid.NewGuid(), CancellationToken.None);

        // Buying a report about one startup must never open another.
        otherTarget.ShouldBeFalse();
    }

    [Fact]
    public async Task Access_is_scoped_to_the_service_it_was_bought_for()
    {
        await SeedAsync(ServiceOrderStatus.Fulfilled, accessDays: 30);

        bool otherService = await CreateSut()
            .HasAsync(_userId, ServiceType.TermSheet, _targetId, CancellationToken.None);

        otherService.ShouldBeFalse();
    }

    [Fact]
    public async Task Access_is_scoped_to_the_buyer()
    {
        await SeedAsync(ServiceOrderStatus.Fulfilled, accessDays: 30);

        bool otherUser = await CreateSut()
            .HasAsync(Guid.NewGuid(), ServiceType.ScoringReport, _targetId, CancellationToken.None);

        otherUser.ShouldBeFalse();
    }

    [Fact]
    public async Task Cached_true_never_outlives_the_access_window()
    {
        // Two days of access left, so the cached answer must not survive longer than that.
        await SeedAsync(ServiceOrderStatus.Fulfilled, accessDays: 30, fulfilledAt: Now.AddDays(-28));

        await CreateSut().HasAsync(_userId, ServiceType.ScoringReport, _targetId, CancellationToken.None);

        string key = CacheKeys.ServiceEntitlement(_userId, (int)ServiceType.ScoringReport, _targetId);
        _cache.LastTtl[key].ShouldBeLessThanOrEqualTo(TimeSpan.FromDays(2));
    }

    [Fact]
    public async Task Permanent_access_is_cached_for_the_default_ttl()
    {
        await SeedAsync(ServiceOrderStatus.Fulfilled, accessDays: 0);

        await CreateSut().HasAsync(_userId, ServiceType.ScoringReport, _targetId, CancellationToken.None);

        string key = CacheKeys.ServiceEntitlement(_userId, (int)ServiceType.ScoringReport, _targetId);
        _cache.LastTtl[key].ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task Invalidate_drops_every_cached_answer_for_the_user()
    {
        await SeedAsync(ServiceOrderStatus.Fulfilled, accessDays: 30);
        ServiceEntitlementChecker sut = CreateSut();
        await sut.HasAsync(_userId, ServiceType.ScoringReport, _targetId, CancellationToken.None);

        await sut.InvalidateAsync(_userId, CancellationToken.None);

        // The order is refunded after the answer was cached; the next read must go back to the database.
        ServiceOrder order = _db.ServiceOrders.Single();
        order.MarkRefunded(Now);
        await _db.SaveChangesAsync();

        bool has = await sut.HasAsync(_userId, ServiceType.ScoringReport, _targetId, CancellationToken.None);
        has.ShouldBeFalse();
    }
}
