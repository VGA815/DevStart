using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Payments;

namespace DevStart.Application.Admin.Subscriptions.GetUserPayments
{
    public sealed record GetUserPaymentsForAdminQuery(Guid UserId) : IQuery<List<AdminPaymentResponse>>;

    public sealed class AdminPaymentResponse
    {
        public Guid Id { get; init; }
        public Guid? SubscriptionId { get; init; }
        public Guid? ServiceOrderId { get; init; }
        public PaymentPurpose Purpose { get; init; }
        public decimal Amount { get; init; }
        public decimal DiscountAmount { get; init; }
        public decimal RefundedAmount { get; init; }
        public string Currency { get; init; } = "RUB";
        public PaymentStatus Status { get; init; }
        public Guid? PromoCodeId { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? PaidAt { get; init; }
    }
}
