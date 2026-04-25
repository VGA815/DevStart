using DevStart.SharedKernel;

namespace DevStart.Domain.InvestmentApplications
{
    public sealed record InvestmentApplicationCreatedDomainEvent(
        Guid ApplicationId,
        Guid InvestorProfileId,
        Guid StartupId,
        decimal Amount) : IDomainEvent;
}
