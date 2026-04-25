using FluentValidation;

namespace DevStart.Application.InvestmentDeals.ConfirmByStartup
{
    internal sealed class ConfirmInvestmentDealByStartupCommandValidator : AbstractValidator<ConfirmInvestmentDealByStartupCommand>
    {
        public ConfirmInvestmentDealByStartupCommandValidator()
        {
            RuleFor(x => x.DealId).NotEmpty();
        }
    }
}
