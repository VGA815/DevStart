using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Payments.Refund;
using DevStart.Application.ServiceOrders;
using DevStart.Application.Subscriptions;
using DevStart.Domain.Payments;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.Payments;

/// <summary>
/// Refund behaviour specific to one-time service orders (SC-49): the refund receipt has to name the
/// service, a proportional refund makes no sense without a period, and the refund must take back what
/// the order delivered.
/// </summary>
public sealed class RefundServiceOrderPaymentTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private const string PromotionDescription = "DevStart — продвижение проекта (разовая услуга)";

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FakePaymentProvider _provider = new();
    private readonly StubServiceEntitlementChecker _entitlements = new();
    private readonly RefundPaymentCommandHandler _sut;

    public RefundServiceOrderPaymentTests()
    {
        var plans = Options.Create(new PlansOptions
        {
            Pro = new PlanOptions
            {
                Price = 990m, Currency = "RUB", DurationDays = 30, Description = "DevStart Pro — 30 days",
            },
        });
        var catalog = Options.Create(new ServiceCatalogOptions
        {
            Items =
            [
                new ServiceCatalogItem
                {
                    ServiceType = ServiceType.Promotion,
                    Price = 1490m,
                    Currency = "RUB",
                    Description = PromotionDescription,
                    AccessDays = 30,
                },
            ],
        });

        _sut = new RefundPaymentCommandHandler(
            _db, _provider, new FixedDateTimeProvider { UtcNow = Now }, plans, catalog,
            new RecordingCacheService(), _entitlements,
            NullLogger<RefundPaymentCommandHandler>.Instance);
    }

    private async Task<(ServiceOrder Order, Payment Payment, Startup Startup)> SeedFulfilledPromotionAsync()
    {
        User user = User.Create("buyer", "buyer@example.com", "hash", Now);
        var startup = new Startup
        {
            Id = Guid.NewGuid(),
            Name = "Acme",
            PublicEmail = "acme@example.com",
            Stage = StartupStage.Seed,
            CreatedAt = Now,
            UpdatedAt = Now,
        };

        ServiceOrder order = ServiceOrder.CreatePending(
            user.Id, ServiceType.Promotion, startup.Id, 1490m, "RUB", Now);
        order.MarkPaid(Now);
        order.MarkFulfilled(Now, accessDays: 30);
        startup.Feature(30, Now);

        Payment payment = Payment.CreatePendingForServiceOrder(
            user.Id, order.Id, PaymentProvider.YooKassa, 1490m, "RUB", Now);
        payment.AssignProviderPayment("pay-svc-1", "https://pay/redirect");
        payment.MarkSucceeded(Now);

        _db.Users.Add(user);
        _db.Startups.Add(startup);
        _db.ServiceOrders.Add(order);
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return (order, payment, startup);
    }

    [Fact]
    public async Task Refund_receipt_names_the_service_not_the_pro_plan()
    {
        (_, Payment payment, _) = await SeedFulfilledPromotionAsync();

        Result result = await _sut.Handle(new RefundPaymentCommand(payment.Id, null), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        // The "возврат прихода" receipt is a fiscal document — printing the Pro plan on a service
        // refund would misstate what was returned.
        _provider.LastRefundInput!.Description.ShouldBe($"Возврат — {PromotionDescription}");
        _provider.LastRefundInput.Description.ShouldNotContain("Pro");
    }

    [Fact]
    public async Task Full_refund_revokes_the_order_and_the_featured_placement()
    {
        (ServiceOrder order, Payment payment, Startup startup) = await SeedFulfilledPromotionAsync();

        Result result = await _sut.Handle(new RefundPaymentCommand(payment.Id, null), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Refunded);
        order.Status.ShouldBe(ServiceOrderStatus.Refunded);
        order.RefundedAt.ShouldBe(Now);
        order.GrantsAccess(Now).ShouldBeFalse();
        startup.FeaturedUntil.ShouldBeNull();
        startup.IsFeatured(Now).ShouldBeFalse();
        _entitlements.InvalidateCount.ShouldBe(1);
    }

    [Fact]
    public async Task Partial_refund_keeps_the_delivery_intact()
    {
        (ServiceOrder order, Payment payment, Startup startup) = await SeedFulfilledPromotionAsync();

        Result result = await _sut.Handle(new RefundPaymentCommand(payment.Id, 100m), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Succeeded);
        order.Status.ShouldBe(ServiceOrderStatus.Fulfilled);
        startup.IsFeatured(Now).ShouldBeTrue();
    }

    [Fact]
    public async Task Proportional_refund_is_rejected_for_a_service_order()
    {
        (_, Payment payment, _) = await SeedFulfilledPromotionAsync();

        Result result = await _sut.Handle(
            new RefundPaymentCommand(payment.Id, null, Proportional: true), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PaymentErrors.ProportionalNotApplicable);
        _provider.LastRefundInput.ShouldBeNull();
    }
}
