using DevStart.Domain.Payments;
using DevStart.SharedKernel;
using Shouldly;

namespace DevStart.UnitTests.Domain.Payments;

public sealed class PaymentRefundTests
{
    private static readonly DateTime CreatedAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

    private static Payment SucceededPayment(decimal amount = 990m)
    {
        Payment payment = Payment.CreatePending(
            Guid.NewGuid(), Guid.NewGuid(), PaymentProvider.YooKassa, amount, "RUB", CreatedAt);
        payment.MarkSucceeded(CreatedAt.AddMinutes(1));
        return payment;
    }

    [Fact]
    public void MarkRefunded_PartialRefund_KeepsSucceededAndRecordsAmount()
    {
        Payment payment = SucceededPayment(990m);

        Result result = payment.MarkRefunded(100m, CreatedAt.AddDays(1));

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Succeeded);
        payment.RefundedAmount.ShouldBe(100m);
        payment.IsFullyRefunded.ShouldBeFalse();
        payment.DomainEvents.OfType<PaymentRefundedDomainEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void MarkRefunded_FullRefund_TransitionsToRefundedAndRaisesEvent()
    {
        Payment payment = SucceededPayment(990m);

        Result result = payment.MarkRefunded(990m, CreatedAt.AddDays(1));

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Refunded);
        payment.RefundedAmount.ShouldBe(990m);
        payment.IsFullyRefunded.ShouldBeTrue();
        PaymentRefundedDomainEvent domainEvent = payment.DomainEvents
            .OfType<PaymentRefundedDomainEvent>()
            .ShouldHaveSingleItem();
        domainEvent.PaymentId.ShouldBe(payment.Id);
        domainEvent.UserId.ShouldBe(payment.UserId);
        domainEvent.RefundedAmount.ShouldBe(990m);
    }

    [Fact]
    public void MarkRefunded_FullRefundTwice_IsIdempotent_RaisesEventOnce()
    {
        Payment payment = SucceededPayment(990m);

        payment.MarkRefunded(990m, CreatedAt.AddDays(1));
        payment.MarkRefunded(990m, CreatedAt.AddDays(2));

        payment.Status.ShouldBe(PaymentStatus.Refunded);
        payment.DomainEvents.OfType<PaymentRefundedDomainEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void MarkRefunded_OnPendingPayment_IsNoOp()
    {
        Payment payment = Payment.CreatePending(
            Guid.NewGuid(), Guid.NewGuid(), PaymentProvider.YooKassa, 990m, "RUB", CreatedAt);

        Result result = payment.MarkRefunded(990m, CreatedAt.AddDays(1));

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Pending);
        payment.RefundedAmount.ShouldBe(0m);
        payment.DomainEvents.OfType<PaymentRefundedDomainEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void MarkRefunded_NegativeAmount_Fails()
    {
        Payment payment = SucceededPayment(990m);

        Result result = payment.MarkRefunded(-1m, CreatedAt.AddDays(1));

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void MarkSucceeded_OnRefundedPayment_DoesNotRevertStatus()
    {
        Payment payment = SucceededPayment(990m);
        payment.MarkRefunded(990m, CreatedAt.AddDays(1));

        Result result = payment.MarkSucceeded(CreatedAt.AddDays(2));

        result.IsSuccess.ShouldBeTrue();
        payment.Status.ShouldBe(PaymentStatus.Refunded);
    }
}
