using DevStart.SharedKernel;

namespace DevStart.Domain.InvestmentDeals
{
    public sealed record InvestmentDealCompletedDomainEvent(
        Guid DealId,
        Guid ApplicationId,
        Guid InvestorProfileId,
        Guid StartupId) : IDomainEvent;
}
