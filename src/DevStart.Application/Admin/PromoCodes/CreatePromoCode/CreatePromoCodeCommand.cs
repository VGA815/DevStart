using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.PromoCodes;
using DevStart.Domain.Subscriptions;

namespace DevStart.Application.Admin.PromoCodes.CreatePromoCode
{
    public sealed record CreatePromoCodeCommand(
        string Code,
        PromoDiscountType DiscountType,
        decimal DiscountValue,
        int? FreePeriodDays,
        SubscriptionPlan Plan,
        int? MaxRedemptions,
        DateTime? ValidFrom,
        DateTime? ValidUntil) : ICommand<Guid>;
}
