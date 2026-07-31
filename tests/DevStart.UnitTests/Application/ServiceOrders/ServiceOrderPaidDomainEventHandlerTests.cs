using DevStart.Application.ServiceOrders;
using DevStart.Application.ServiceOrders.Paid;
using DevStart.Domain.Notifications;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.Startups;
using DevStart.Infrastructure.Database;
using DevStart.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.ServiceOrders;

public sealed class ServiceOrderPaidDomainEventHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly RecordingNotificationService _notifications = new();
    private readonly StubServiceEntitlementChecker _entitlements = new();
    private readonly RecordingCacheService _cache = new();
    private readonly Guid _userId = Guid.NewGuid();
    private Guid _startupId;

    private ServiceOrderPaidDomainEventHandler CreateSut()
    {
        var catalog = Options.Create(new ServiceCatalogOptions
        {
            Items =
            [
                new ServiceCatalogItem { ServiceType = ServiceType.ScoringReport, Price = 490m, AccessDays = 30 },
                new ServiceCatalogItem { ServiceType = ServiceType.TermSheet, Price = 990m, AccessDays = 0 },
                new ServiceCatalogItem { ServiceType = ServiceType.Promotion, Price = 1490m, AccessDays = 30 },
            ],
        });

        return new ServiceOrderPaidDomainEventHandler(
            _db,
            _notifications,
            _entitlements,
            _cache,
            catalog,
            new FixedDateTimeProvider { UtcNow = Now },
            NullLogger<ServiceOrderPaidDomainEventHandler>.Instance);
    }

    private async Task<ServiceOrder> SeedPaidOrderAsync(ServiceType serviceType)
    {
        var startup = new Startup
        {
            Id = Guid.NewGuid(),
            Name = "Acme",
            PublicEmail = "acme@example.com",
            Stage = StartupStage.Seed,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        _startupId = startup.Id;

        ServiceOrder order = ServiceOrder.CreatePending(
            _userId, serviceType, startup.Id, 490m, "RUB", Now);
        order.MarkPaid(Now);

        _db.Startups.Add(startup);
        _db.ServiceOrders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    private ServiceOrderPaidDomainEvent EventFor(ServiceOrder order)
        => new(order.Id, order.UserId, order.ServiceType, order.TargetId);

    [Fact]
    public async Task Paid_scoring_report_is_fulfilled_with_a_bounded_access_window()
    {
        ServiceOrder order = await SeedPaidOrderAsync(ServiceType.ScoringReport);

        await CreateSut().Handle(EventFor(order), CancellationToken.None);

        ServiceOrder stored = _db.ServiceOrders.Single();
        stored.Status.ShouldBe(ServiceOrderStatus.Fulfilled);
        stored.FulfilledAt.ShouldBe(Now);
        stored.ExpiresAt.ShouldBe(Now.AddDays(30));
        stored.GrantsAccess(Now).ShouldBeTrue();
        stored.GrantsAccess(Now.AddDays(31)).ShouldBeFalse();
    }

    [Fact]
    public async Task Paid_term_sheet_grants_permanent_access()
    {
        ServiceOrder order = await SeedPaidOrderAsync(ServiceType.TermSheet);

        await CreateSut().Handle(EventFor(order), CancellationToken.None);

        ServiceOrder stored = _db.ServiceOrders.Single();
        stored.Status.ShouldBe(ServiceOrderStatus.Fulfilled);
        stored.ExpiresAt.ShouldBeNull();
        stored.GrantsAccess(Now.AddYears(5)).ShouldBeTrue();
    }

    [Fact]
    public async Task Paid_promotion_features_the_target_startup()
    {
        ServiceOrder order = await SeedPaidOrderAsync(ServiceType.Promotion);

        await CreateSut().Handle(EventFor(order), CancellationToken.None);

        Startup startup = _db.Startups.Single(s => s.Id == _startupId);
        startup.FeaturedUntil.ShouldBe(Now.AddDays(30));
        startup.IsFeatured(Now).ShouldBeTrue();
        startup.IsFeatured(Now.AddDays(31)).ShouldBeFalse();
    }

    [Fact]
    public async Task Fulfillment_drops_the_cached_entitlement_answer()
    {
        ServiceOrder order = await SeedPaidOrderAsync(ServiceType.ScoringReport);

        await CreateSut().Handle(EventFor(order), CancellationToken.None);

        // Without this, a "no access" answer cached moments earlier would hold back what was just paid for.
        _entitlements.InvalidateCount.ShouldBe(1);
    }

    [Fact]
    public async Task Buyer_is_notified_with_the_target_and_the_access_window()
    {
        ServiceOrder order = await SeedPaidOrderAsync(ServiceType.ScoringReport);

        await CreateSut().Handle(EventFor(order), CancellationToken.None);

        Notification notification = _notifications.Published.Single();
        notification.UserId.ShouldBe(_userId);
        notification.Type.ShouldBe(NotificationType.ServiceOrderFulfilled);
        notification.ReferenceId.ShouldBe(order.Id);
        notification.Body.ShouldContain("Acme");
        notification.Body.ShouldContain("01.07.2026");
    }

    [Fact]
    public async Task Replayed_event_does_not_deliver_twice()
    {
        ServiceOrder order = await SeedPaidOrderAsync(ServiceType.Promotion);
        ServiceOrderPaidDomainEvent domainEvent = EventFor(order);

        await CreateSut().Handle(domainEvent, CancellationToken.None);
        await CreateSut().Handle(domainEvent, CancellationToken.None);

        // A duplicated webhook must not extend the promotion or notify the buyer a second time.
        _db.Startups.Single(s => s.Id == _startupId).FeaturedUntil.ShouldBe(Now.AddDays(30));
        _notifications.Published.Count.ShouldBe(1);
    }
}
