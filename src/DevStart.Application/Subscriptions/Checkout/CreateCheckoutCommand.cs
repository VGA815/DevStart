using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Subscriptions;

namespace DevStart.Application.Subscriptions.Checkout
{
    public sealed class CreateCheckoutCommand : ICommand<CheckoutResponse>
    {
        public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Pro;

        public CreateCheckoutCommand() { }
        public CreateCheckoutCommand(SubscriptionPlan plan)
        {
            Plan = plan;
        }
    }

    public sealed class CheckoutResponse
    {
        public Guid SubscriptionId { get; init; }
        public Guid PaymentId { get; init; }
        public string ConfirmationUrl { get; init; } = null!;
    }
}
