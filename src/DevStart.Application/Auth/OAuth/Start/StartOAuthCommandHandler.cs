using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using System.Security.Cryptography;

namespace DevStart.Application.Auth.OAuth.Start
{
    internal sealed class StartOAuthCommandHandler(
        IExternalAuthProviderFactory providerFactory,
        IPkceGenerator pkceGenerator,
        IOAuthStateStore stateStore)
        : ICommandHandler<StartOAuthCommand, StartOAuthResponse>
    {
        private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(10);

        public async Task<Result<StartOAuthResponse>> Handle(
            StartOAuthCommand command,
            CancellationToken cancellationToken)
        {
            IExternalAuthProvider provider = providerFactory.Get(command.Provider);

            string redirectUri = string.IsNullOrWhiteSpace(command.RedirectUri)
                ? string.Empty
                : command.RedirectUri;

            PkcePair pkce = pkceGenerator.Create();
            string state = GenerateState();

            var entry = new OAuthStateEntry(
                command.Provider,
                pkce.Verifier,
                redirectUri,
                command.LinkUserId);

            await stateStore.SaveAsync(state, entry, StateTtl, cancellationToken);

            string url = provider.BuildAuthorizationUrl(state, pkce.Challenge, redirectUri);

            return new StartOAuthResponse(url, state);
        }

        private static string GenerateState()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncode(bytes);
        }

        private static string Base64UrlEncode(byte[] bytes) =>
            Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
    }
}
