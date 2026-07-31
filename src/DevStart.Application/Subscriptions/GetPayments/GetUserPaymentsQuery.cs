using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Payments;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.Subscriptions;

namespace DevStart.Application.Subscriptions.GetPayments
{
    public sealed record GetUserPaymentsQuery() : IQuery<List<PaymentHistoryResponse>>;

    public sealed class PaymentHistoryResponse
    {
        public Guid Id { get; init; }
        public Guid? SubscriptionId { get; init; }
        public Guid? ServiceOrderId { get; init; }
        public PaymentPurpose Purpose { get; init; }

        /// <summary>The plan this payment bought — null for a one-time service order, which has no plan.</summary>
        public SubscriptionPlan? Plan { get; init; }

        /// <summary>The service this payment bought — null for a subscription payment.</summary>
        public ServiceType? ServiceType { get; init; }
        public decimal Amount { get; init; }
        public decimal RefundedAmount { get; init; }
        public string Currency { get; init; } = "RUB";
        public PaymentStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? PaidAt { get; init; }
    }
}
