using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestmentApplications.Accept
{
    public sealed class AcceptInvestmentApplicationCommand : ICommand<Guid>
    {
        public Guid ApplicationId { get; set; }

        public AcceptInvestmentApplicationCommand(Guid applicationId)
        {
            ApplicationId = applicationId;
        }
    }
}
