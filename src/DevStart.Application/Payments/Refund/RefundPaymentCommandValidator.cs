using FluentValidation;

namespace DevStart.Application.Payments.Refund
{
    internal sealed class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
    {
        public RefundPaymentCommandValidator()
        {
            RuleFor(x => x.PaymentId).NotEmpty();
            RuleFor(x => x.Amount!.Value)
                .GreaterThan(0m)
                .When(x => x.Amount.HasValue);
        }
    }
}
