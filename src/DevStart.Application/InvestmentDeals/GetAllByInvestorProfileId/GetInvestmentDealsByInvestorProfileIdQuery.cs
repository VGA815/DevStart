using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestmentDeals.GetAllByInvestorProfileId
{
    public sealed class GetInvestmentDealsByInvestorProfileIdQuery : IQuery<List<InvestmentDealResponse>>
    {
        public Guid InvestorProfileId { get; set; }

        public GetInvestmentDealsByInvestorProfileIdQuery(Guid investorProfileId)
        {
            InvestorProfileId = investorProfileId;
        }
    }
}
