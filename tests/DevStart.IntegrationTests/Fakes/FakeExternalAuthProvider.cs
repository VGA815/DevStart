using DevStart.Application.Abstractions.Authentication;
using DevStart.Domain.ExternalLogins;

namespace DevStart.IntegrationTests.Fakes
{
    /// <summary>Fake OAuth provider. <see cref="BuildAuthorizationUrl"/> returns a deterministic URL and
    /// <see cref="ExchangeCodeAsync"/> returns a configurable identity, so the OAuth start/callback flow
    /// can be driven end-to-end without contacting Google/GitHub.</summary>
    internal sealed class FakeExternalAuthProvider : IExternalAuthProvider
    {
        public ExternalLoginProvider Provider { get; init; } = ExternalLoginProvider.Google;

        public ExternalUserInfo? Result { get; set; }
        public Exception? Throws { get; set; }

        public string BuildAuthorizationUrl(string state, string codeChallenge, string redirectUri)
            => $"https://provider.test.local/authorize?state={state}&code_challenge={codeChallenge}&redirect_uri={Uri.EscapeDataString(redirectUri)}";

        public Task<ExternalUserInfo> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
        {
            if (Throws is not null)
            {
                throw Throws;
            }

            return Task.FromResult(Result ?? throw new InvalidOperationException("FakeExternalAuthProvider.Result not set"));
        }
    }

    internal sealed class FakeExternalAuthProviderFactory(IEnumerable<IExternalAuthProvider> providers) : IExternalAuthProviderFactory
    {
        public IExternalAuthProvider Get(ExternalLoginProvider provider) =>
            providers.FirstOrDefault(p => p.Provider == provider)
            ?? throw new InvalidOperationException($"No fake registered for {provider}");
    }
}
