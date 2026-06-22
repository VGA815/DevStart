namespace DevStart.Domain.PromoCodes
{
    public enum PromoDiscountType
    {
        /// <summary>Percentage off the plan price (<see cref="PromoCode.DiscountValue"/> in 1..100).</summary>
        Percentage = 0,

        /// <summary>Fixed amount (in the plan currency) off the plan price.</summary>
        FixedAmount = 1,

        /// <summary>Activates the plan for free for <see cref="PromoCode.FreePeriodDays"/> days, no payment.</summary>
        FreePeriod = 2,
    }
}
