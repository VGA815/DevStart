using DevStart.SharedKernel;

namespace DevStart.Domain.InvestmentApplications
{
    public sealed record InvestmentApplicationRejectedDomainEvent(
        Guid ApplicationId,
        Guid InvestorProfileId,
        Guid StartupId) : IDomainEvent;
}
