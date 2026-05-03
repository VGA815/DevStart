using DevStart.Domain.InvestmentApplications;
using FluentValidation;

namespace DevStart.Application.InvestmentApplications.Create
{
    internal sealed class CreateInvestmentApplicationCommandValidator : AbstractValidator<CreateInvestmentApplicationCommand>
    {
        public CreateInvestmentApplicationCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Message).MaximumLength(2000);
            RuleFor(x => x.Instrument).IsInEnum();

            RuleFor(x => x.LiquidationPreference)
                .InclusiveBetween(1.0m, 3.0m);

            // Safe / Convertible: ValuationCap > 0
            RuleFor(x => x.ValuationCap)
                .NotNull()
                .GreaterThan(0)
                .When(x => x.Instrument == InvestmentInstrument.Safe || x.Instrument == InvestmentInstrument.ConvertibleLoan);

            // Discount optional, but if set must be 0–0.5
            RuleFor(x => x.Discount)
                .InclusiveBetween(0m, 0.5m)
                .When(x => x.Discount.HasValue);

            // Convertible: InterestRate required (0–0.30), TermMonths required (6–60)
            RuleFor(x => x.InterestRate)
                .NotNull()
                .InclusiveBetween(0m, 0.30m)
                .When(x => x.Instrument == InvestmentInstrument.ConvertibleLoan);

            RuleFor(x => x.TermMonths)
                .NotNull()
                .InclusiveBetween(6, 60)
                .When(x => x.Instrument == InvestmentInstrument.ConvertibleLoan);

            // Priced: PreMoneyValuation > 0
            RuleFor(x => x.PreMoneyValuation)
                .NotNull()
                .GreaterThan(0)
                .When(x => x.Instrument == InvestmentInstrument.PricedRound);
        }
    }
}
