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
    ///
    /// Exactly one of <see cref="Tokens"/>, <see cref="Consent"/>, <see cref="TwoFactor"/> and
    /// <see cref="TwoFactorSetup"/> is non-null. <see cref="TrustedDevice"/> is not part of that
    /// choice: it is an optional extra that accompanies <see cref="Tokens"/> or <see cref="Consent"/>
    /// when the user asked to be remembered on this browser.
    /// </summary>
    public sealed record OAuthAuthResult(
        TokenPair? Tokens,
        ConsentChallenge? Consent,
        TwoFactorChallenge? TwoFactor,
        TwoFactorSetupChallenge? TwoFactorSetup,
        TrustedDeviceGrant? TrustedDevice = null)
    {
        public static OAuthAuthResult Authenticated(TokenPair tokens) => new(tokens, null, null, null);

        public static OAuthAuthResult ConsentRequired(ConsentChallenge challenge) => new(null, challenge, null, null);

        public static OAuthAuthResult TwoFactorRequired(TwoFactorChallenge challenge) => new(null, null, challenge, null);

        public static OAuthAuthResult TwoFactorSetupRequired(TwoFactorSetupChallenge challenge) => new(null, null, null, challenge);

        /// <summary>Attaches a freshly minted device grant; a null grant leaves the result untouched.</summary>
        public OAuthAuthResult WithTrustedDevice(TrustedDeviceGrant? grant)
            => grant is null ? this : this with { TrustedDevice = grant };
    }

    /// <summary>
    /// The "remember this device" secret, handed to the client exactly once. Deliberately not part of
    /// <see cref="TokenPair"/>: that type is the response body of the refresh endpoint, where a device
    /// token would be meaningless.
    /// </summary>
    public sealed record TrustedDeviceGrant(string DeviceToken, Guid DeviceId, DateTime ExpiresAt);

    public sealed record ConsentChallenge(string PendingToken, IReadOnlyList<RequiredConsent> Required);

    public sealed record TwoFactorChallenge(string PendingToken);

    public sealed record TwoFactorSetupChallenge(string PendingToken);
}
