using DevStart.Domain.PromoCodes;
using FluentValidation;

namespace DevStart.Application.Admin.PromoCodes.CreatePromoCode
{
    internal sealed class CreatePromoCodeCommandValidator : AbstractValidator<CreatePromoCodeCommand>
    {
        public CreatePromoCodeCommandValidator()
        {
            RuleFor(c => c.Code)
                .NotEmpty()
                .MaximumLength(64)
                .Matches("^[A-Za-z0-9_-]+$")
                .WithMessage("Code may contain only letters, digits, '-' and '_'.");

            RuleFor(c => c.DiscountType).IsInEnum();
            RuleFor(c => c.Plan).IsInEnum();

            RuleFor(c => c.DiscountValue)
                .InclusiveBetween(1, 100)
                .When(c => c.DiscountType == PromoDiscountType.Percentage)
                .WithMessage("Percentage discount must be between 1 and 100.");

            RuleFor(c => c.DiscountValue)
                .GreaterThan(0)
                .When(c => c.DiscountType == PromoDiscountType.FixedAmount)
                .WithMessage("Fixed discount must be greater than 0.");

            RuleFor(c => c.FreePeriodDays)
                .NotNull()
                .GreaterThan(0)
                .LessThanOrEqualTo(3650)
                .When(c => c.DiscountType == PromoDiscountType.FreePeriod)
                .WithMessage("Free period must be a positive number of days.");

            RuleFor(c => c.MaxRedemptions)
                .GreaterThan(0)
                .When(c => c.MaxRedemptions.HasValue);

            RuleFor(c => c.ValidUntil)
                .GreaterThan(c => c.ValidFrom!.Value)
                .When(c => c.ValidFrom.HasValue && c.ValidUntil.HasValue)
                .WithMessage("ValidUntil must be after ValidFrom.");
        }
    }
}
