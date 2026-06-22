using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Subscriptions;

namespace DevStart.Application.Subscriptions.Checkout
{
    public sealed class CreateCheckoutCommand : ICommand<CheckoutResponse>
    {
        public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Pro;

        /// <summary>Optional promo code to apply at checkout.</summary>
        public string? PromoCode { get; set; }

        public CreateCheckoutCommand() { }
        public CreateCheckoutCommand(SubscriptionPlan plan)
        {
            Plan = plan;
        }
        public CreateCheckoutCommand(SubscriptionPlan plan, string? promoCode)
        {
            Plan = plan;
            PromoCode = promoCode;
        }
    }

    public sealed class CheckoutResponse
    {
        public Guid SubscriptionId { get; init; }

        /// <summary>The created payment, or <see cref="Guid.Empty"/> when the plan was activated for free.</summary>
        public Guid PaymentId { get; init; }

        /// <summary>Provider redirect URL, or null when the subscription was activated immediately (free promo).</summary>
        public string? ConfirmationUrl { get; init; }

        /// <summary>True when a free/comp promo activated the subscription without a payment.</summary>
        public bool Activated { get; init; }
    }
}
