using FluentValidation;

namespace DevStart.Application.Admin.Subscriptions.RevokeSubscription
{
    internal sealed class RevokeSubscriptionCommandValidator : AbstractValidator<RevokeSubscriptionCommand>
    {
        public RevokeSubscriptionCommandValidator()
        {
            RuleFor(c => c.SubscriptionId).NotEmpty();
            RuleFor(c => c.Reason)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(1000);
        }
    }
}
