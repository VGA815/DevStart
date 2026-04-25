using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestmentDeals.ConfirmByInvestor
{
    public sealed class ConfirmInvestmentDealByInvestorCommand : ICommand
    {
        public Guid DealId { get; set; }

        public ConfirmInvestmentDealByInvestorCommand(Guid dealId)
        {
            DealId = dealId;
        }
    }
}
