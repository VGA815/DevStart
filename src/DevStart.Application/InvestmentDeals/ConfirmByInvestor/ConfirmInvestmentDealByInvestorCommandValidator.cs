using FluentValidation;

namespace DevStart.Application.InvestmentDeals.ConfirmByInvestor
{
    internal sealed class ConfirmInvestmentDealByInvestorCommandValidator : AbstractValidator<ConfirmInvestmentDealByInvestorCommand>
    {
        public ConfirmInvestmentDealByInvestorCommandValidator()
        {
            RuleFor(x => x.DealId).NotEmpty();
        }
    }
}
