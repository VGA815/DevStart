using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestmentApplications.Reject
{
    public sealed class RejectInvestmentApplicationCommand : ICommand
    {
        public Guid ApplicationId { get; set; }

        public RejectInvestmentApplicationCommand(Guid applicationId)
        {
            ApplicationId = applicationId;
        }
    }
}
