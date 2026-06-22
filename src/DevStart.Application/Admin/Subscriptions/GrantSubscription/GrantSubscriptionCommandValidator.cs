using FluentValidation;

namespace DevStart.Application.Admin.Subscriptions.GrantSubscription
{
    internal sealed class GrantSubscriptionCommandValidator : AbstractValidator<GrantSubscriptionCommand>
    {
        public GrantSubscriptionCommandValidator()
        {
            RuleFor(c => c.UserId).NotEmpty();
            RuleFor(c => c.DurationDays)
                .GreaterThan(0)
                .LessThanOrEqualTo(3650)
                .When(c => c.DurationDays.HasValue);
            RuleFor(c => c.Reason)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(1000);
        }
    }
}
