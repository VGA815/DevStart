using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Payments.Sync;
using DevStart.Application.Subscriptions;
using DevStart.Domain.Payments;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.Subscriptions;
using DevStart.Infrastructure.Database;
using DevStart.Infrastructure.Payments;
using DevStart.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Infrastructure.Payments;

public sealed class PaymentReconciliationJobTests
{
    private static readonly DateTime Now = new(2026, 5, 26, 12, 0, 0, DateTimeKind.Utc);
    private const decimal Amount = 990m;

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FakePaymentProvider _provider = new();
    private readonly PaymentReconciliationJob _job;

    public PaymentReconciliationJobTests()
    {
        var clock = new FixedDateTimeProvider { UtcNow = Now };
        var plans = Options.Create(new PlansOptions
        {
            Pro = new PlanOptions { Price = Amount, Currency = "RUB", DurationDays = 30, Description = "DevStart Pro" },
        });
        var sync = new SyncPaymentStatusCommandHandler(
            _db, _provider, clock, plans, new RecordingCacheService(), new StubServiceEntitlementChecker(),
            NullLogger<SyncPaymentStatusCommandHandler>.Instance);
        var options = Options.Create(new BillingMaintenanceOptions());
        _job = new PaymentReconciliationJob(
            _db, sync, clock, options, NullLogger<PaymentReconciliationJob>.Instance);
    }

    [Fact]
    public async Task RefundFallback_CapturedPaymentRefundedAtProvider_GetsCancelledLocally()
    {
        // A succeeded payment whose refund.succeeded webhook was missed: locally still Succeeded with
        // no recorded refund, paid recently (inside the refund-reconcile window).
        Subscription subscription = Subscription.CreatePending(Guid.NewGuid(), SubscriptionPlan.Pro, Now);
        subscription.Activate(Now, 30);
        Payment payment = Payment.CreatePending(subscription.UserId, subscription.Id, PaymentProvider.YooKassa, Amount, "RUB", Now);
        payment.AssignProviderPayment("pay-1", "https://pay/redirect");
        payment.MarkSucceeded(Now);
        _db.Subscriptions.Add(subscription);
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        // Provider now reports it fully refunded.
        _provider.SnapshotToReturn = new ProviderPaymentSnapshot(
            "pay-1", PaymentStatus.Succeeded, Paid: true, Now, RefundedAmount: Amount, "succeeded");

        await _job.ReconcilePendingAsync(CancellationToken.None);

        _db.Payments.Single().Status.ShouldBe(PaymentStatus.Refunded);
        _db.Subscriptions.Single().Status.ShouldBe(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public async Task AbandonedSweep_PendingPastReconcileWindow_StillPending_IsCancelled()
    {
        // Pending for 100h (older than the 72h reconcile window) — abandoned.
        DateTime createdAt = Now.AddHours(-100);
        Subscription subscription = Subscription.CreatePending(Guid.NewGuid(), SubscriptionPlan.Pro, createdAt);
        Payment payment = Payment.CreatePending(subscription.UserId, subscription.Id, PaymentProvider.YooKassa, Amount, "RUB", createdAt);
        payment.AssignProviderPayment("pay-2", "https://pay/redirect");
        _db.Subscriptions.Add(subscription);
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        // Final authoritative check still says pending → the sweep cancels it.
        _provider.SnapshotToReturn = new ProviderPaymentSnapshot(
            "pay-2", PaymentStatus.Pending, Paid: false, null, RefundedAmount: 0m, null);

        await _job.ReconcilePendingAsync(CancellationToken.None);

        _db.Payments.Single().Status.ShouldBe(PaymentStatus.Cancelled);
        _db.Subscriptions.Single().Status.ShouldBe(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public async Task AbandonedSweep_PendingServiceOrder_IsCancelledAlongsideItsPayment()
    {
        // An abandoned one-time service order used to sit Pending forever: the sweep cancelled the
        // payment and the subscription but never looked at the order.
        DateTime createdAt = Now.AddHours(-100);
        Guid userId = Guid.NewGuid();
        ServiceOrder order = ServiceOrder.CreatePending(
            userId, ServiceType.ScoringReport, Guid.NewGuid(), 490m, "RUB", createdAt);
        Payment payment = Payment.CreatePendingForServiceOrder(
            userId, order.Id, PaymentProvider.YooKassa, 490m, "RUB", createdAt);
        payment.AssignProviderPayment("pay-svc", "https://pay/redirect");
        _db.ServiceOrders.Add(order);
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        _provider.SnapshotToReturn = new ProviderPaymentSnapshot(
            "pay-svc", PaymentStatus.Pending, Paid: false, null, RefundedAmount: 0m, null);

        await _job.ReconcilePendingAsync(CancellationToken.None);

        _db.Payments.Single().Status.ShouldBe(PaymentStatus.Cancelled);
        _db.ServiceOrders.Single().Status.ShouldBe(ServiceOrderStatus.Cancelled);
        _db.ServiceOrders.Single().CancelledAt.ShouldBe(Now);
    }

    [Fact]
    public async Task AbandonedSweep_PendingPastWindow_ActuallySucceeded_IsActivatedNotCancelled()
    {
        // The final sync must never throw away a late success.
        DateTime createdAt = Now.AddHours(-100);
        Subscription subscription = Subscription.CreatePending(Guid.NewGuid(), SubscriptionPlan.Pro, createdAt);
        Payment payment = Payment.CreatePending(subscription.UserId, subscription.Id, PaymentProvider.YooKassa, Amount, "RUB", createdAt);
        payment.AssignProviderPayment("pay-3", "https://pay/redirect");
        _db.Subscriptions.Add(subscription);
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        _provider.SnapshotToReturn = new ProviderPaymentSnapshot(
            "pay-3", PaymentStatus.Succeeded, Paid: true, Now, RefundedAmount: 0m, "succeeded");

        await _job.ReconcilePendingAsync(CancellationToken.None);

        _db.Payments.Single().Status.ShouldBe(PaymentStatus.Succeeded);
        _db.Subscriptions.Single().Status.ShouldBe(SubscriptionStatus.Active);
    }
}
