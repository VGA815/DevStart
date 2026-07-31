using FluentValidation;

namespace DevStart.Application.Admin.ServiceOrders.CancelServiceOrder
{
    internal sealed class CancelServiceOrderCommandValidator : AbstractValidator<CancelServiceOrderCommand>
    {
        public CancelServiceOrderCommandValidator()
        {
            RuleFor(x => x.ServiceOrderId).NotEmpty();
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        }
    }
}
