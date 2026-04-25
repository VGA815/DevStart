using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestmentApplications.GetAllByInvestorProfileId
{
    public sealed class GetInvestmentApplicationsByInvestorProfileIdQuery : IQuery<List<InvestmentApplicationResponse>>
    {
        public Guid InvestorProfileId { get; set; }

        public GetInvestmentApplicationsByInvestorProfileIdQuery(Guid investorProfileId)
        {
            InvestorProfileId = investorProfileId;
        }
    }
}
