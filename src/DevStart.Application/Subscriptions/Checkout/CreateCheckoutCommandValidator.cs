using FluentValidation;
using DevStart.Domain.Subscriptions;

namespace DevStart.Application.Subscriptions.Checkout
{
    internal sealed class CreateCheckoutCommandValidator : AbstractValidator<CreateCheckoutCommand>
    {
        public CreateCheckoutCommandValidator()
        {
            RuleFor(x => x.Plan)
                .Equal(SubscriptionPlan.Pro);
        }
    }
}
