using DevStart.Domain.PromoCodes;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Shouldly;

namespace DevStart.UnitTests.Domain.PromoCodes;

public sealed class PromoCodeTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private static PromoCode Percentage(decimal value) => PromoCode.Create(
        "save", PromoDiscountType.Percentage, value, null, SubscriptionPlan.Pro, null, null, null, Guid.NewGuid(), Now);

    [Fact]
    public void Create_NormalizesCode_ToUpper()
    {
        PromoCode promo = Percentage(50m);
        promo.Code.ShouldBe("SAVE");
        promo.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void ComputeCheckout_Percentage_AppliesDiscount()
    {
        PromoCheckout checkout = Percentage(50m).ComputeCheckout(1000m);

        checkout.IsFree.ShouldBeFalse();
        checkout.Amount.ShouldBe(500m);
        checkout.Discount.ShouldBe(500m);
    }

    [Fact]
    public void ComputeCheckout_HundredPercent_IsFree()
    {
        PromoCheckout checkout = Percentage(100m).ComputeCheckout(1000m);

        checkout.IsFree.ShouldBeTrue();
        checkout.Amount.ShouldBe(0m);
    }

    [Fact]
    public void ComputeCheckout_FixedAmount_CapsAtBase()
    {
        PromoCode promo = PromoCode.Create(
            "minus", PromoDiscountType.FixedAmount, 1500m, null, SubscriptionPlan.Pro, null, null, null, Guid.NewGuid(), Now);

        PromoCheckout checkout = promo.ComputeCheckout(1000m);

        checkout.IsFree.ShouldBeTrue();
        checkout.Discount.ShouldBe(1000m);
    }

    [Fact]
    public void ComputeCheckout_FreePeriod_IsFree_WithDays()
    {
        PromoCode promo = PromoCode.Create(
            "trial", PromoDiscountType.FreePeriod, 0m, 14, SubscriptionPlan.Pro, null, null, null, Guid.NewGuid(), Now);

        PromoCheckout checkout = promo.ComputeCheckout(1000m);

        checkout.IsFree.ShouldBeTrue();
        checkout.FreeDays.ShouldBe(14);
    }

    [Fact]
    public void Validate_Active_Succeeds()
    {
        Percentage(50m).Validate(SubscriptionPlan.Pro, Now, alreadyRedeemedByUser: false)
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Inactive_Fails()
    {
        PromoCode promo = Percentage(50m);
        promo.Deactivate();

        promo.Validate(SubscriptionPlan.Pro, Now, false).Error.ShouldBe(PromoCodeErrors.Inactive);
    }

    [Fact]
    public void Validate_Expired_Fails()
    {
        PromoCode promo = PromoCode.Create(
            "old", PromoDiscountType.Percentage, 50m, null, SubscriptionPlan.Pro, null, null, Now.AddDays(-1), Guid.NewGuid(), Now.AddDays(-10));

        promo.Validate(SubscriptionPlan.Pro, Now, false).Error.ShouldBe(PromoCodeErrors.Expired);
    }

    [Fact]
    public void Validate_GlobalLimitReached_Fails()
    {
        PromoCode promo = PromoCode.Create(
            "limited", PromoDiscountType.Percentage, 50m, null, SubscriptionPlan.Pro, maxRedemptions: 1, null, null, Guid.NewGuid(), Now);
        promo.RegisterRedemption();

        promo.Validate(SubscriptionPlan.Pro, Now, false).Error.ShouldBe(PromoCodeErrors.GlobalLimitReached);
    }

    [Fact]
    public void Validate_AlreadyRedeemedByUser_Fails()
    {
        Percentage(50m).Validate(SubscriptionPlan.Pro, Now, alreadyRedeemedByUser: true)
            .Error.ShouldBe(PromoCodeErrors.AlreadyRedeemedByUser);
    }
}
