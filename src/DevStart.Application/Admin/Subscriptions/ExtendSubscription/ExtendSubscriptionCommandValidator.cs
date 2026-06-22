using FluentValidation;

namespace DevStart.Application.Admin.Subscriptions.ExtendSubscription
{
    internal sealed class ExtendSubscriptionCommandValidator : AbstractValidator<ExtendSubscriptionCommand>
    {
        public ExtendSubscriptionCommandValidator()
        {
            RuleFor(c => c.SubscriptionId).NotEmpty();
            RuleFor(c => c.AdditionalDays)
                .GreaterThan(0)
                .LessThanOrEqualTo(3650);
            RuleFor(c => c.Reason)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(1000);
        }
    }
}
