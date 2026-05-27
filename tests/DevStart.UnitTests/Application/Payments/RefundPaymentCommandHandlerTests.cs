using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Payments.Refund;
using DevStart.Application.Subscriptions;
using DevStart.Domain.Payments;
using DevStart.Domain.Subscriptions;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.Payments;

public sealed class RefundPaymentCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 26, 12, 0, 0, DateTimeKind.Utc);
    private const decimal Amount = 990m;

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FakePaymentProvider _provider = new();
    private readonly RefundPaymentCommandHandler _sut;

    public RefundPaymentCommandHandlerTests()
    {
        var plans = Options.Create(new PlansOptions
        {
            Pro = new PlanOptions { Price = Amount, Currency = "RUB", DurationDays = 30, Description = "DevStart Pro" },
        });
        _sut = new RefundPaymentCommandHandler(
            _db, _provider, new FixedDateTimeProvider { UtcNow = Now }, plans,
            NullLogger<RefundPaymentCommandHandler>.Instance);
    }

    private async Task<(Subscription, Payment)> SeedAsync(PaymentStatus paymentStatus = PaymentStatus.Succeeded)
    {
        User user = User.Create("buyer", "buyer@example.com", "hash", Now);
        Subscription subscription = Subscription.CreatePending(user.Id, SubscriptionPlan.Pro, Now);
        subscription.Activate(Now, 30);

        Payment payment = Payment.CreatePending(user.Id, subscription.Id, PaymentProvider.YooKassa, Amount, "RUB", Now);
        payment.AssignProviderPayment("pay-1", "https://pay/redirect");
        if (paymentStatus == PaymentStatus.Succeeded)
        {
            payment.MarkSucceeded(Now);
        }

        _db.Users.Add(user);
        _db.Subscriptions.Add(subscription);
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return (subscription, payment);
    }

    [Fact]
    public async Task FullRefund_ProviderSucceeded_RefundsPaymentAndCancelsSubscriptionImmediately()
    {
        (Subscription subscription, Payment payment) = await SeedAsync();
        _provider.RefundSucceededToReturn = true;

        Result result = await _sut.Handle(new RefundPaymentCommand(payment.Id, null), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Refunded);
        payment.RefundedAmount.ShouldBe(Amount);
        subscription.Status.ShouldBe(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public async Task PartialRefund_ProviderSucceeded_RecordsAmountButKeepsAccess()
    {
        (Subscription subscription, Payment payment) = await SeedAsync();
        _provider.RefundSucceededToReturn = true;

        Result result = await _sut.Handle(new RefundPaymentCommand(payment.Id, 100m), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Succeeded);
        payment.RefundedAmount.ShouldBe(100m);
        subscription.Status.ShouldBe(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task FullRefund_ProviderPending_LeavesLocalStateForWebhookOrReconciliation()
    {
        (Subscription subscription, Payment payment) = await SeedAsync();
        _provider.RefundSucceededToReturn = false;

        Result result = await _sut.Handle(new RefundPaymentCommand(payment.Id, null), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Succeeded);
        payment.RefundedAmount.ShouldBe(0m);
        subscription.Status.ShouldBe(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task ProviderThrowsTransient_ReturnsProviderUnavailable()
    {
        (_, Payment payment) = await SeedAsync();
        _provider.CreateRefundException = new PaymentProviderException("provider down", isTransient: true);

        Result result = await _sut.Handle(new RefundPaymentCommand(payment.Id, null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Payments.ProviderUnavailable");
        payment.Status.ShouldBe(PaymentStatus.Succeeded);
        payment.RefundedAmount.ShouldBe(0m);
    }

    [Fact]
    public async Task ProviderThrowsNonTransient_ReturnsProviderError()
    {
        (_, Payment payment) = await SeedAsync();
        _provider.CreateRefundException = new PaymentProviderException("rejected", isTransient: false);

        Result result = await _sut.Handle(new RefundPaymentCommand(payment.Id, null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Payments.ProviderError");
    }

    [Fact]
    public async Task Refund_NonSucceededPayment_Fails()
    {
        (_, Payment payment) = await SeedAsync(PaymentStatus.Pending);

        Result result = await _sut.Handle(new RefundPaymentCommand(payment.Id, null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        _provider.LastRefundInput.ShouldBeNull();
    }
}
