using DevStart.Domain.ServiceOrders;
using FluentValidation;

namespace DevStart.Application.ServiceOrders.Checkout
{
    internal sealed class CreateServiceOrderCheckoutCommandValidator
        : AbstractValidator<CreateServiceOrderCheckoutCommand>
    {
        public CreateServiceOrderCheckoutCommandValidator()
        {
            RuleFor(x => x.ServiceType).IsInEnum();

            // Every service currently sold is bought for something; the handler then checks that the
            // target exists and that the buyer is allowed to buy it for that target.
            RuleFor(x => x.TargetId)
                .NotEmpty()
                .When(x => ServiceTargets.RequiresTarget(x.ServiceType))
                .WithMessage("targetId is required for this service.");
        }
    }
}
