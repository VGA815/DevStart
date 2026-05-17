using DevStart.Domain.Payments;
using Shouldly;

namespace DevStart.UnitTests.Domain.Payments;

public sealed class PaymentTests
{
    private static readonly DateTime CreatedAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreatePending_ShouldInitializePendingPayment()
    {
        Guid userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid subscriptionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        Payment payment = Payment.CreatePending(userId, subscriptionId, PaymentProvider.YooKassa, 990m, "RUB", CreatedAt);

        payment.Id.ShouldNotBe(Guid.Empty);
        payment.UserId.ShouldBe(userId);
        payment.SubscriptionId.ShouldBe(subscriptionId);
        payment.Provider.ShouldBe(PaymentProvider.YooKassa);
        payment.Amount.ShouldBe(990m);
        payment.Currency.ShouldBe("RUB");
        payment.Status.ShouldBe(PaymentStatus.Pending);
        payment.ProviderPaymentId.ShouldBeNull();
        payment.ConfirmationUrl.ShouldBeNull();
        payment.CreatedAt.ShouldBe(CreatedAt);
        payment.PaidAt.ShouldBeNull();
    }

    [Fact]
    public void AssignProviderPayment_ShouldStoreProviderIdentifiers()
    {
        Payment payment = CreatePendingPayment();

        payment.AssignProviderPayment("provider-1", "https://pay.example.com");

        payment.ProviderPaymentId.ShouldBe("provider-1");
        payment.ConfirmationUrl.ShouldBe("https://pay.example.com");
    }

    [Fact]
    public void MarkSucceeded_ShouldSetStatusAndPaidAt()
    {
        Payment payment = CreatePendingPayment();
        DateTime paidAt = CreatedAt.AddMinutes(5);

        var result = payment.MarkSucceeded(paidAt);

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Succeeded);
        payment.PaidAt.ShouldBe(paidAt);
    }

    [Fact]
    public void MarkSucceeded_ShouldBeIdempotent_WhenAlreadySucceeded()
    {
        Payment payment = CreatePendingPayment();
        DateTime paidAt = CreatedAt.AddMinutes(5);
        payment.MarkSucceeded(paidAt);

        var result = payment.MarkSucceeded(paidAt.AddMinutes(1));

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Succeeded);
        payment.PaidAt.ShouldBe(paidAt);
    }

    [Theory]
    [InlineData(PaymentStatus.Cancelled)]
    [InlineData(PaymentStatus.Failed)]
    public void MarkSucceeded_ShouldFail_WhenPaymentIsTerminal(PaymentStatus terminalStatus)
    {
        Payment payment = CreatePendingPayment();
        if (terminalStatus == PaymentStatus.Cancelled)
        {
            payment.MarkCancelled(CreatedAt.AddMinutes(1));
        }
        else
        {
            payment.MarkFailed(CreatedAt.AddMinutes(1));
        }

        var result = payment.MarkSucceeded(CreatedAt.AddMinutes(2));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Payments.ProviderError");
        payment.Status.ShouldBe(terminalStatus);
        payment.PaidAt.ShouldBeNull();
    }

    [Fact]
    public void MarkCancelledAndMarkFailed_ShouldBeIdempotent()
    {
        Payment cancelled = CreatePendingPayment();
        Payment failed = CreatePendingPayment();

        cancelled.MarkCancelled(CreatedAt.AddMinutes(1));
        var cancelledAgain = cancelled.MarkCancelled(CreatedAt.AddMinutes(2));
        failed.MarkFailed(CreatedAt.AddMinutes(1));
        var failedAgain = failed.MarkFailed(CreatedAt.AddMinutes(2));

        cancelledAgain.IsSuccess.ShouldBeTrue();
        cancelled.Status.ShouldBe(PaymentStatus.Cancelled);
        failedAgain.IsSuccess.ShouldBeTrue();
        failed.Status.ShouldBe(PaymentStatus.Failed);
    }

    private static Payment CreatePendingPayment() =>
        Payment.CreatePending(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            PaymentProvider.YooKassa,
            990m,
            "RUB",
            CreatedAt);
}
