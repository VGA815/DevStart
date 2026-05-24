using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Payments;
using DevStart.Domain.Subscriptions;

namespace DevStart.Application.Subscriptions.GetPayments
{
    public sealed record GetUserPaymentsQuery() : IQuery<List<PaymentHistoryResponse>>;

    public sealed class PaymentHistoryResponse
    {
        public Guid Id { get; init; }
        public Guid SubscriptionId { get; init; }
        public SubscriptionPlan Plan { get; init; }
        public decimal Amount { get; init; }
        public decimal RefundedAmount { get; init; }
        public string Currency { get; init; } = "RUB";
        public PaymentStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? PaidAt { get; init; }
    }
}
