using FluentValidation;

namespace DevStart.Application.InvestmentApplications.Reject
{
    internal sealed class RejectInvestmentApplicationCommandValidator : AbstractValidator<RejectInvestmentApplicationCommand>
    {
        public RejectInvestmentApplicationCommandValidator()
        {
            RuleFor(x => x.ApplicationId).NotEmpty();
        }
    }
}
