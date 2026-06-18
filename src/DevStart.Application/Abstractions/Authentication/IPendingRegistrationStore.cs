using DevStart.Domain.ExternalLogins;

namespace DevStart.Application.Abstractions.Authentication
{
    /// <summary>
    /// A short-lived record of an OAuth identity awaiting consent acceptance before a user account is
    /// created (new registration) or before tokens are issued (existing user with outdated consents).
    /// </summary>
    public sealed record PendingExternalRegistration(
        ExternalLoginProvider Provider,
        string ProviderUserId,
        string Email,
        bool EmailVerified,
        string? Name,
        Guid? ExistingUserId);

    public interface IPendingRegistrationStore
    {
        Task SaveAsync(string token, PendingExternalRegistration entry, TimeSpan ttl, CancellationToken cancellationToken);

        Task<PendingExternalRegistration?> ConsumeAsync(string token, CancellationToken cancellationToken);
    }
}
