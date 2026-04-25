using DevStart.SharedKernel;

namespace DevStart.Domain.InvestmentApplications
{
    public sealed record InvestmentApplicationWithdrawnDomainEvent(
        Guid ApplicationId,
        Guid InvestorProfileId,
        Guid StartupId) : IDomainEvent;
}
