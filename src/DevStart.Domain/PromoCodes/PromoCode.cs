using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;

namespace DevStart.Domain.PromoCodes
{
    /// <summary>
    /// Result of applying a promo code to a plan price. <see cref="IsFree"/> means the subscription can be
    /// activated immediately without a payment; <see cref="FreeDays"/> overrides the default plan duration
    /// (null = use the plan default).
    /// </summary>
    public readonly record struct PromoCheckout(decimal Amount, decimal Discount, bool IsFree, int? FreeDays);

    public sealed class PromoCode : Entity
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public PromoDiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public int? FreePeriodDays { get; set; }
        public SubscriptionPlan Plan { get; set; }
        public int? MaxRedemptions { get; set; }
        public int RedeemedCount { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedByUserId { get; set; }

        public PromoCode() { }

        public static string Normalize(string code) => code.Trim().ToUpperInvariant();

        public static PromoCode Create(
            string code,
            PromoDiscountType discountType,
            decimal discountValue,
            int? freePeriodDays,
            SubscriptionPlan plan,
            int? maxRedemptions,
            DateTime? validFrom,
            DateTime? validUntil,
            Guid createdByUserId,
            DateTime utcNow)
            => new()
            {
                Id = Guid.NewGuid(),
                Code = Normalize(code),
                DiscountType = discountType,
                DiscountValue = discountType == PromoDiscountType.FreePeriod ? 0m : discountValue,
                FreePeriodDays = discountType == PromoDiscountType.FreePeriod ? freePeriodDays : null,
                Plan = plan,
                MaxRedemptions = maxRedemptions,
                RedeemedCount = 0,
                ValidFrom = validFrom,
                ValidUntil = validUntil,
                IsActive = true,
                CreatedAt = utcNow,
                CreatedByUserId = createdByUserId,
            };

        /// <summary>
        /// Checks whether this code can be redeemed right now for the given plan by a user who has
        /// (<paramref name="alreadyRedeemedByUser"/>) or has not previously redeemed it.
        /// </summary>
        public Result Validate(SubscriptionPlan plan, DateTime utcNow, bool alreadyRedeemedByUser)
        {
            if (!IsActive)
            {
                return Result.Failure(PromoCodeErrors.Inactive);
            }
            if (Plan != plan)
            {
                return Result.Failure(PromoCodeErrors.PlanMismatch);
            }
            if (ValidFrom is not null && utcNow < ValidFrom)
            {
                return Result.Failure(PromoCodeErrors.NotYetValid);
            }
            if (ValidUntil is not null && utcNow > ValidUntil)
            {
                return Result.Failure(PromoCodeErrors.Expired);
            }
            if (MaxRedemptions is not null && RedeemedCount >= MaxRedemptions)
            {
                return Result.Failure(PromoCodeErrors.GlobalLimitReached);
            }
            if (alreadyRedeemedByUser)
            {
                return Result.Failure(PromoCodeErrors.AlreadyRedeemedByUser);
            }
            return Result.Success();
        }

        public PromoCheckout ComputeCheckout(decimal baseAmount)
        {
            switch (DiscountType)
            {
                case PromoDiscountType.FreePeriod:
                    return new PromoCheckout(0m, baseAmount, IsFree: true, FreeDays: FreePeriodDays);

                case PromoDiscountType.Percentage:
                {
                    decimal discount = Math.Round(baseAmount * DiscountValue / 100m, 2, MidpointRounding.AwayFromZero);
                    decimal amount = baseAmount - discount;
                    return amount <= 0m
                        ? new PromoCheckout(0m, baseAmount, IsFree: true, FreeDays: null)
                        : new PromoCheckout(amount, discount, IsFree: false, FreeDays: null);
                }

                case PromoDiscountType.FixedAmount:
                {
                    decimal discount = Math.Min(DiscountValue, baseAmount);
                    decimal amount = baseAmount - discount;
                    return amount <= 0m
                        ? new PromoCheckout(0m, baseAmount, IsFree: true, FreeDays: null)
                        : new PromoCheckout(amount, discount, IsFree: false, FreeDays: null);
                }

                default:
                    return new PromoCheckout(baseAmount, 0m, IsFree: false, FreeDays: null);
            }
        }

        public void RegisterRedemption() => RedeemedCount++;

        public void Deactivate() => IsActive = false;
    }
}
