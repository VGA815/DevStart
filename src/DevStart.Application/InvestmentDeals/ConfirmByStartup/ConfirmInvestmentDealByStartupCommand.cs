using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestmentDeals.ConfirmByStartup
{
    public sealed class ConfirmInvestmentDealByStartupCommand : ICommand
    {
        public Guid DealId { get; set; }

        public ConfirmInvestmentDealByStartupCommand(Guid dealId)
        {
            DealId = dealId;
        }
    }
}
