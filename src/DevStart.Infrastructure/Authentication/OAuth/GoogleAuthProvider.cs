using DevStart.Application.Abstractions.Authentication;
using DevStart.Domain.ExternalLogins;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace DevStart.Infrastructure.Authentication.OAuth
{
    internal sealed class GoogleAuthProvider : IExternalAuthProvider
    {
        private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string DiscoveryUrl = "https://accounts.google.com/.well-known/openid-configuration";
        private const string ValidIssuer = "https://accounts.google.com";

        private readonly HttpClient _httpClient;
        private readonly GoogleOAuthOptions _options;
        private static readonly ConfigurationManager<OpenIdConnectConfiguration> ConfigManager =
            new(DiscoveryUrl, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever());

        public GoogleAuthProvider(HttpClient httpClient, IOptions<GoogleOAuthOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public ExternalLoginProvider Provider => ExternalLoginProvider.Google;

        public string BuildAuthorizationUrl(string state, string codeChallenge, string redirectUri)
        {
            string scopes = _options.Scopes is { Length: > 0 }
                ? string.Join(' ', _options.Scopes)
                : "openid email profile";

            string finalRedirect = string.IsNullOrWhiteSpace(redirectUri) ? _options.RedirectUri : redirectUri;

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["response_type"] = "code";
            query["client_id"] = _options.ClientId;
            query["redirect_uri"] = finalRedirect;
            query["scope"] = scopes;
            query["state"] = state;
            query["code_challenge"] = codeChallenge;
            query["code_challenge_method"] = "S256";
            query["access_type"] = "offline";
            query["prompt"] = "select_account";

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
                ["code"] = code,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["redirect_uri"] = finalRedirect,
                ["grant_type"] = "authorization_code",
                ["code_verifier"] = codeVerifier,
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(form),
            };
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            GoogleTokenResponse? token = await response.Content
                .ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken);

            if (token is null || string.IsNullOrEmpty(token.IdToken))
            {
                throw new InvalidOperationException("Google returned no id_token");
            }

            OpenIdConnectConfiguration config = await ConfigManager.GetConfigurationAsync(cancellationToken);

            var handler = new JsonWebTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = ValidIssuer,
                ValidateIssuer = true,
                ValidAudience = _options.ClientId,
                ValidateAudience = true,
                ValidateLifetime = true,
                IssuerSigningKeys = config.SigningKeys,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(2),
            };

            TokenValidationResult result = await handler.ValidateTokenAsync(token.IdToken, validationParameters);
            if (!result.IsValid)
            {
                throw new InvalidOperationException("Google id_token validation failed", result.Exception);
            }

            IDictionary<string, object> claims = result.Claims;

            string sub = GetString(claims, "sub") ?? throw new InvalidOperationException("Google id_token missing sub");
            string? email = GetString(claims, "email");
            bool emailVerified = GetBool(claims, "email_verified");
            string? name = GetString(claims, "name");
            string? picture = GetString(claims, "picture");

            return new ExternalUserInfo(sub, email, emailVerified, name, picture);
        }

        private static string? GetString(IDictionary<string, object> claims, string key) =>
            claims.TryGetValue(key, out object? value) ? value?.ToString() : null;

        private static bool GetBool(IDictionary<string, object> claims, string key)
        {
            if (!claims.TryGetValue(key, out object? value) || value is null) return false;
            return value switch
            {
                bool b => b,
                string s => bool.TryParse(s, out bool parsed) && parsed,
                _ => false,
            };
        }

        private sealed class GoogleTokenResponse
        {
            [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
            [JsonPropertyName("id_token")] public string? IdToken { get; set; }
            [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
            [JsonPropertyName("token_type")] public string? TokenType { get; set; }
            [JsonPropertyName("scope")] public string? Scope { get; set; }
            [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        }
    }
}
