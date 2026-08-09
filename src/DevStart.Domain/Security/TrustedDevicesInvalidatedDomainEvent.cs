using DevStart.SharedKernel;

namespace DevStart.Domain.Security
{
    /// <summary>
    /// Raised when a user tightens (or otherwise changes) their 2FA policy in a way that must not
    /// leave previously trusted devices standing.
    /// </summary>
    public sealed record TrustedDevicesInvalidatedDomainEvent(Guid UserId) : IDomainEvent;
}
