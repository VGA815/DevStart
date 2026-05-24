using DevStart.SharedKernel;

namespace DevStart.Domain.Payments
{
    public sealed record PaymentRefundedDomainEvent(
        Guid PaymentId,
        Guid UserId,
        Guid SubscriptionId,
        decimal RefundedAmount) : IDomainEvent;
}
