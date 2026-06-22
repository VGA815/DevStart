using DevStart.SharedKernel;

namespace DevStart.Domain.PromoCodes
{
    /// <summary>
    /// One redemption of a <see cref="PromoCode"/> by a user. A unique (promo_code_id, user_id) index
    /// enforces once-per-user. <see cref="PaymentId"/> is null for free/comp activations that skip payment.
    /// </summary>
    public sealed class PromoCodeRedemption : Entity
    {
        public Guid Id { get; set; }
        public Guid PromoCodeId { get; set; }
        public Guid UserId { get; set; }
        public Guid? PaymentId { get; set; }
        public Guid SubscriptionId { get; set; }
        public decimal DiscountApplied { get; set; }
        public DateTime RedeemedAt { get; set; }

        public PromoCodeRedemption() { }

        public static PromoCodeRedemption Create(
            Guid promoCodeId,
            Guid userId,
            Guid subscriptionId,
            Guid? paymentId,
            decimal discountApplied,
            DateTime redeemedAt)
            => new()
            {
                Id = Guid.NewGuid(),
                PromoCodeId = promoCodeId,
                UserId = userId,
                SubscriptionId = subscriptionId,
                PaymentId = paymentId,
                DiscountApplied = discountApplied,
                RedeemedAt = redeemedAt,
            };
    }
}
