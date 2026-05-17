using DevStart.Application.Abstractions.Authentication;
using DevStart.Domain.ExternalLogins;

namespace DevStart.UnitTests.TestSupport
{
    internal sealed class FakeExternalAuthProvider : IExternalAuthProvider
    {
        public ExternalLoginProvider Provider { get; init; } = ExternalLoginProvider.Google;
        public Func<string, string, string, CancellationToken, Task<ExternalUserInfo>>? OnExchange { get; set; }
        public ExternalUserInfo? Result { get; set; }
        public Exception? Throws { get; set; }

        public string BuildAuthorizationUrl(string state, string codeChallenge, string redirectUri)
            => $"https://provider.example/auth?state={state}&code_challenge={codeChallenge}";

        public Task<ExternalUserInfo> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
        {
            if (Throws is not null) throw Throws;
            if (OnExchange is not null) return OnExchange(code, codeVerifier, redirectUri, cancellationToken);
            return Task.FromResult(Result ?? throw new InvalidOperationException("FakeExternalAuthProvider.Result not set"));
        }
    }

    internal sealed class FakeExternalAuthProviderFactory : IExternalAuthProviderFactory
    {
        private readonly IExternalAuthProvider _provider;

        public FakeExternalAuthProviderFactory(IExternalAuthProvider provider)
        {
            _provider = provider;
        }

        public IExternalAuthProvider Get(ExternalLoginProvider provider) =>
            provider == _provider.Provider
                ? _provider
                : throw new InvalidOperationException($"No fake registered for {provider}");
    }
}
