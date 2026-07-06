using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.UserConsents;

namespace DevStart.Application.Auth.OAuth
{
    /// <summary>
    /// Result of a first-factor authentication (password login or OAuth callback): an authenticated
    /// token pair, or exactly one pending challenge the client must satisfy before tokens are
    /// issued — consent acceptance (via the complete-registration endpoint), a TOTP code
    /// (via POST api/auth/2fa/verify), or mandatory 2FA enrollment for admins
    /// (via POST api/auth/2fa/setup + setup/confirm).
    /// </summary>
    public sealed record OAuthAuthResult(
        TokenPair? Tokens,
        ConsentChallenge? Consent,
        TwoFactorChallenge? TwoFactor,
        TwoFactorSetupChallenge? TwoFactorSetup)
    {
        public static OAuthAuthResult Authenticated(TokenPair tokens) => new(tokens, null, null, null);

        public static OAuthAuthResult ConsentRequired(ConsentChallenge challenge) => new(null, challenge, null, null);

        public static OAuthAuthResult TwoFactorRequired(TwoFactorChallenge challenge) => new(null, null, challenge, null);

        public static OAuthAuthResult TwoFactorSetupRequired(TwoFactorSetupChallenge challenge) => new(null, null, null, challenge);
    }

    public sealed record ConsentChallenge(string PendingToken, IReadOnlyList<RequiredConsent> Required);

    public sealed record TwoFactorChallenge(string PendingToken);

    public sealed record TwoFactorSetupChallenge(string PendingToken);
}
