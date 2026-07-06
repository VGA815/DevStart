using DevStart.Domain.ExternalLogins;

namespace DevStart.Application.Abstractions.Authentication
{
    /// <summary>
    /// A short-lived record of an OAuth identity awaiting consent acceptance before a user account is
    /// created (new registration) or before tokens are issued (existing user with outdated consents).
    /// <see cref="TwoFactorSatisfied"/> is true when the record was created after the user passed the
    /// 2FA gate, so the completion handler must not challenge again.
    /// </summary>
    public sealed record PendingExternalRegistration(
        ExternalLoginProvider Provider,
        string ProviderUserId,
        string Email,
        bool EmailVerified,
        string? Name,
        Guid? ExistingUserId,
        bool TwoFactorSatisfied = false);

    public interface IPendingRegistrationStore
    {
        Task SaveAsync(string token, PendingExternalRegistration entry, TimeSpan ttl, CancellationToken cancellationToken);

        Task<PendingExternalRegistration?> ConsumeAsync(string token, CancellationToken cancellationToken);
    }
}
