using DevStart.Application.Abstractions.Authentication;
using DevStart.Domain.ExternalLogins;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace DevStart.Infrastructure.Authentication.OAuth
{
    internal sealed class GitHubAuthProvider : IExternalAuthProvider
    {
        private const string AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        private const string TokenEndpoint = "https://github.com/login/oauth/access_token";
        private const string UserEndpoint = "https://api.github.com/user";
        private const string UserEmailsEndpoint = "https://api.github.com/user/emails";
        private const string UserAgentValue = "DevStart-OAuth";

        private readonly HttpClient _httpClient;
        private readonly GitHubOAuthOptions _options;

        public GitHubAuthProvider(HttpClient httpClient, IOptions<GitHubOAuthOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public ExternalLoginProvider Provider => ExternalLoginProvider.GitHub;

        public string BuildAuthorizationUrl(string state, string codeChallenge, string redirectUri)
        {
            string scopes = _options.Scopes is { Length: > 0 }
                ? string.Join(' ', _options.Scopes)
                : "read:user user:email";

            string finalRedirect = string.IsNullOrWhiteSpace(redirectUri) ? _options.RedirectUri : redirectUri;

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["client_id"] = _options.ClientId;
            query["redirect_uri"] = finalRedirect;
            query["scope"] = scopes;
            query["state"] = state;
            query["code_challenge"] = codeChallenge;
            query["code_challenge_method"] = "S256";
            query["allow_signup"] = "true";

            return $"{AuthorizationEndpoint}?{query}";
        }

        public async Task<ExternalUserInfo> ExchangeCodeAsync(
            string code,
            string codeVerifier,
            string redirectUri,
            CancellationToken cancellationToken)
        {
            string finalRedirect = string.IsNullOrWhiteSpace(redirectUri) ? _options.RedirectUri : redirectUri;

            var form = new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = finalRedirect,
                ["code_verifier"] = codeVerifier,
            };

            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(form),
            };
            tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using HttpResponseMessage tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
            tokenResponse.EnsureSuccessStatusCode();

            GitHubTokenResponse? token = await tokenResponse.Content
                .ReadFromJsonAsync<GitHubTokenResponse>(cancellationToken);

            if (token is null || string.IsNullOrEmpty(token.AccessToken))
            {
                throw new InvalidOperationException("GitHub returned no access_token");
            }

            GitHubUser? user = await GetAsync<GitHubUser>(UserEndpoint, token.AccessToken, cancellationToken)
                ?? throw new InvalidOperationException("GitHub returned no user");

            string? email = user.Email;
            bool emailVerified = !string.IsNullOrEmpty(email);

            if (string.IsNullOrEmpty(email))
            {
                List<GitHubEmail>? emails = await GetAsync<List<GitHubEmail>>(
                    UserEmailsEndpoint, token.AccessToken, cancellationToken);

                if (emails is not null)
                {
                    GitHubEmail? primary = emails.FirstOrDefault(e => e.Primary && e.Verified)
                        ?? emails.FirstOrDefault(e => e.Verified)
                        ?? emails.FirstOrDefault(e => e.Primary);
                    if (primary is not null)
                    {
                        email = primary.Email;
                        emailVerified = primary.Verified;
                    }
                }
            }

            return new ExternalUserInfo(
                user.Id.ToString(),
                email,
                emailVerified,
                user.Name ?? user.Login,
                user.AvatarUrl);
        }

        private async Task<T?> GetAsync<T>(string url, string accessToken, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.UserAgent.ParseAdd(UserAgentValue);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }

        private sealed class GitHubTokenResponse
        {
            [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
            [JsonPropertyName("token_type")] public string? TokenType { get; set; }
            [JsonPropertyName("scope")] public string? Scope { get; set; }
        }

        private sealed class GitHubUser
        {
            [JsonPropertyName("id")] public long Id { get; set; }
            [JsonPropertyName("login")] public string Login { get; set; } = string.Empty;
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("email")] public string? Email { get; set; }
            [JsonPropertyName("avatar_url")] public string? AvatarUrl { get; set; }
        }

        private sealed class GitHubEmail
        {
            [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
            [JsonPropertyName("primary")] public bool Primary { get; set; }
            [JsonPropertyName("verified")] public bool Verified { get; set; }
        }
    }
}
