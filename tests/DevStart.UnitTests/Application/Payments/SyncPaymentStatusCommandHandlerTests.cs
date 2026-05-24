using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Payments.Sync;
using DevStart.Application.Subscriptions;
using DevStart.Domain.Payments;
using DevStart.Domain.Subscriptions;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.Payments;

public sealed class SyncPaymentStatusCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc);
    private const string ProviderPaymentId = "pay-1";

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FakePaymentProvider _provider = new();
    private readonly SyncPaymentStatusCommandHandler _sut;

    public SyncPaymentStatusCommandHandlerTests()
    {
        var plans = Options.Create(new PlansOptions
        {
            Pro = new PlanOptions { Price = 990m, Currency = "RUB", DurationDays = 30, Description = "DevStart Pro" },
        });
        _sut = new SyncPaymentStatusCommandHandler(
            _db, _provider, new FixedDateTimeProvider { UtcNow = Now }, plans,
            NullLogger<SyncPaymentStatusCommandHandler>.Instance);
    }

    private async Task<(Subscription, Payment)> SeedAsync(
        SubscriptionStatus subscriptionStatus = SubscriptionStatus.Pending,
        PaymentStatus paymentStatus = PaymentStatus.Pending)
    {
        Guid userId = Guid.NewGuid();
        Subscription subscription = Subscription.CreatePending(userId, SubscriptionPlan.Pro, Now);
        if (subscriptionStatus == SubscriptionStatus.Active)
        {
            subscription.Activate(Now, 30);
        }

        Payment payment = Payment.CreatePending(userId, subscription.Id, PaymentProvider.YooKassa, 990m, "RUB", Now);
        payment.AssignProviderPayment(ProviderPaymentId, "https://pay/redirect");
        if (paymentStatus == PaymentStatus.Succeeded)
        {
            payment.MarkSucceeded(Now);
        }

        _db.Subscriptions.Add(subscription);
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return (subscription, payment);
    }

    [Fact]
    public async Task Succeeded_ActivatesSubscriptionAndMarksPaid()
    {
        await SeedAsync();
        _provider.SnapshotToReturn = new ProviderPaymentSnapshot(
            ProviderPaymentId, PaymentStatus.Succeeded, Paid: true, Now, RefundedAmount: 0m, "succeeded");

        Result result = await _sut.Handle(new SyncPaymentStatusCommand(ProviderPaymentId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _db.Payments.Single().Status.ShouldBe(PaymentStatus.Succeeded);
        Subscription subscription = _db.Subscriptions.Single();
        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.ExpiresAt.ShouldBe(Now.AddDays(30));
    }

    [Fact]
    public async Task Canceled_CancelsPaymentAndSubscription()
    {
        await SeedAsync();
        _provider.SnapshotToReturn = new ProviderPaymentSnapshot(
            ProviderPaymentId, PaymentStatus.Cancelled, Paid: false, null, RefundedAmount: 0m, null);

        await _sut.Handle(new SyncPaymentStatusCommand(ProviderPaymentId), CancellationToken.None);

        _db.Payments.Single().Status.ShouldBe(PaymentStatus.Cancelled);
        _db.Subscriptions.Single().Status.ShouldBe(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public async Task FullRefund_RefundsPaymentAndCancelsSubscription()
    {
        await SeedAsync(SubscriptionStatus.Active, PaymentStatus.Succeeded);
        _provider.SnapshotToReturn = new ProviderPaymentSnapshot(
            ProviderPaymentId, PaymentStatus.Succeeded, Paid: true, Now, RefundedAmount: 990m, "succeeded");

        await _sut.Handle(new SyncPaymentStatusCommand(ProviderPaymentId), CancellationToken.None);

        _db.Payments.Single().Status.ShouldBe(PaymentStatus.Refunded);
        _db.Subscriptions.Single().Status.ShouldBe(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public async Task NullSnapshot_IsNoOp()
    {
        await SeedAsync();
        _provider.SnapshotToReturn = null;

        Result result = await _sut.Handle(new SyncPaymentStatusCommand(ProviderPaymentId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _db.Payments.Single().Status.ShouldBe(PaymentStatus.Pending);
        _db.Subscriptions.Single().Status.ShouldBe(SubscriptionStatus.Pending);
    }

    [Fact]
    public async Task Succeeded_RunTwice_IsIdempotent()
    {
        await SeedAsync();
        _provider.SnapshotToReturn = new ProviderPaymentSnapshot(
            ProviderPaymentId, PaymentStatus.Succeeded, Paid: true, Now, RefundedAmount: 0m, "succeeded");

        await _sut.Handle(new SyncPaymentStatusCommand(ProviderPaymentId), CancellationToken.None);
        DateTime firstExpiry = _db.Subscriptions.Single().ExpiresAt;
        Result second = await _sut.Handle(new SyncPaymentStatusCommand(ProviderPaymentId), CancellationToken.None);

        second.IsSuccess.ShouldBeTrue();
        Subscription subscription = _db.Subscriptions.Single();
        subscription.Status.ShouldBe(SubscriptionStatus.Active);
        subscription.ExpiresAt.ShouldBe(firstExpiry);
    }
}
