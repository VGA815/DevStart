using DevStart.Application.Auth.OAuth;
using DevStart.Domain.Users;

namespace DevStart.Application.Auth.TwoFactor
{
    /// <summary>
    /// The 2FA gate shared by every token-issuance site (password login, OAuth callback, OAuth
    /// completion). Runs after the ban check and before the consent gate.
    /// </summary>
    public interface ITwoFactorLoginGate
    {
        /// <summary>
        /// Returns a <see cref="OAuthAuthResult.TwoFactorRequired"/> challenge when the user has 2FA
        /// enabled, a <see cref="OAuthAuthResult.TwoFactorSetupRequired"/> challenge when the user is
        /// an admin without 2FA (enrollment is mandatory for admins), or null when no challenge is
        /// needed and the caller may proceed to issue tokens.
        /// </summary>
        Task<OAuthAuthResult?> ChallengeIfRequiredAsync(
            User user,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken);
    }
}
