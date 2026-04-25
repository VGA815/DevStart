using FluentValidation;

namespace DevStart.Application.InvestmentApplications.Accept
{
    internal sealed class AcceptInvestmentApplicationCommandValidator : AbstractValidator<AcceptInvestmentApplicationCommand>
    {
        public AcceptInvestmentApplicationCommandValidator()
        {
            RuleFor(x => x.ApplicationId).NotEmpty();
        }
    }
}
