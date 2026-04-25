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
        }
    }
}
