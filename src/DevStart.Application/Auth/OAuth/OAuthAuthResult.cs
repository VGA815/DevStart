using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.UserConsents;

namespace DevStart.Application.Auth.OAuth
{
    /// <summary>
    /// Result of an OAuth callback: either an authenticated token pair, or a consent challenge that
    /// the client must satisfy (via the complete-registration endpoint) before tokens are issued.
    /// </summary>
    public sealed record OAuthAuthResult(TokenPair? Tokens, ConsentChallenge? Consent)
    {
        public static OAuthAuthResult Authenticated(TokenPair tokens) => new(tokens, null);

        public static OAuthAuthResult ConsentRequired(ConsentChallenge challenge) => new(null, challenge);
    }

    public sealed record ConsentChallenge(string PendingToken, IReadOnlyList<RequiredConsent> Required);
}
