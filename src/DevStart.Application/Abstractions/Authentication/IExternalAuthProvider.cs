using DevStart.Domain.ExternalLogins;

namespace DevStart.Application.Abstractions.Authentication
{
    public interface IExternalAuthProvider
    {
        ExternalLoginProvider Provider { get; }

        string BuildAuthorizationUrl(string state, string codeChallenge, string redirectUri);

        Task<ExternalUserInfo> ExchangeCodeAsync(
            string code,
            string codeVerifier,
            string redirectUri,
            CancellationToken cancellationToken);
    }
}
