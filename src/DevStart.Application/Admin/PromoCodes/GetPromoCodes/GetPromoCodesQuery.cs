using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.PromoCodes;
using DevStart.Domain.Subscriptions;

namespace DevStart.Application.Admin.PromoCodes.GetPromoCodes
{
    public sealed record GetPromoCodesQuery(
        bool? ActiveOnly = null,
        int PageNumber = 1,
        int PageSize = 50) : IQuery<List<PromoCodeResponse>>;

    public sealed class PromoCodeResponse
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = null!;
        public PromoDiscountType DiscountType { get; init; }
        public decimal DiscountValue { get; init; }
        public int? FreePeriodDays { get; init; }
        public SubscriptionPlan Plan { get; init; }
        public int? MaxRedemptions { get; init; }
        public int RedeemedCount { get; init; }
        public DateTime? ValidFrom { get; init; }
        public DateTime? ValidUntil { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
