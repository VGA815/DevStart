namespace DevStart.Infrastructure.Authentication.OAuth
{
    public abstract class OAuthProviderOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string[] Scopes { get; set; } = Array.Empty<string>();
    }

    public sealed class GoogleOAuthOptions : OAuthProviderOptions
    {
    }

    public sealed class GitHubOAuthOptions : OAuthProviderOptions
    {
    }
}
