using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestmentApplications.Withdraw
{
    public sealed class WithdrawInvestmentApplicationCommand : ICommand
    {
        public Guid ApplicationId { get; set; }

        public WithdrawInvestmentApplicationCommand(Guid applicationId)
        {
            ApplicationId = applicationId;
        }
    }
}
