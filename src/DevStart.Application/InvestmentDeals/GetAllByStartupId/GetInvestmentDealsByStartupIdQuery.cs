using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestmentDeals.GetAllByStartupId
{
    public sealed class GetInvestmentDealsByStartupIdQuery : IQuery<List<InvestmentDealResponse>>
    {
        public Guid StartupId { get; set; }

        public GetInvestmentDealsByStartupIdQuery(Guid startupId)
        {
            StartupId = startupId;
        }
    }
}
