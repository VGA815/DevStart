using FluentValidation;

namespace DevStart.Application.InvestmentApplications.Withdraw
{
    internal sealed class WithdrawInvestmentApplicationCommandValidator : AbstractValidator<WithdrawInvestmentApplicationCommand>
    {
        public WithdrawInvestmentApplicationCommandValidator()
        {
            RuleFor(x => x.ApplicationId).NotEmpty();
        }
    }
}
