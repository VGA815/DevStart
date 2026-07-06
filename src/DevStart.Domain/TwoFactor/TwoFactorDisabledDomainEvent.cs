using DevStart.SharedKernel;

namespace DevStart.Domain.TwoFactor
{
    public sealed record TwoFactorDisabledDomainEvent(Guid UserId, bool ResetByAdmin) : IDomainEvent;
}
