using DevStart.SharedKernel;

namespace DevStart.Domain.TwoFactor
{
    public sealed record TwoFactorEnabledDomainEvent(Guid UserId) : IDomainEvent;
}
