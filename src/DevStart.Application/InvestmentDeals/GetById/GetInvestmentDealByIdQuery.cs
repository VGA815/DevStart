using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestmentDeals.GetById
{
    public sealed class GetInvestmentDealByIdQuery : IQuery<InvestmentDealResponse>
    {
        public Guid DealId { get; set; }

        public GetInvestmentDealByIdQuery(Guid dealId)
        {
            DealId = dealId;
        }
    }
}
