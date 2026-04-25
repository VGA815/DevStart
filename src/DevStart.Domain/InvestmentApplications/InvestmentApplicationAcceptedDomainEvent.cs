using DevStart.SharedKernel;

namespace DevStart.Domain.InvestmentApplications
{
    public sealed record InvestmentApplicationAcceptedDomainEvent(
        Guid ApplicationId,
        Guid DealId,
        Guid InvestorProfileId,
        Guid StartupId) : IDomainEvent;
}
