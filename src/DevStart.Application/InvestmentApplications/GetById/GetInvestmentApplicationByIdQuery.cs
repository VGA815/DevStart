using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestmentApplications.GetById
{
    public sealed class GetInvestmentApplicationByIdQuery : IQuery<InvestmentApplicationResponse>
    {
        public Guid ApplicationId { get; set; }

        public GetInvestmentApplicationByIdQuery(Guid applicationId)
        {
            ApplicationId = applicationId;
        }
    }
}
