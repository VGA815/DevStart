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

            // Proportional refunds derive the amount from the unused period; an explicit amount is
            // ambiguous alongside it.
            RuleFor(x => x.Amount)
                .Null()
                .When(x => x.Proportional)
                .WithMessage("Amount must not be provided for a proportional refund.");
        }
    }
}
