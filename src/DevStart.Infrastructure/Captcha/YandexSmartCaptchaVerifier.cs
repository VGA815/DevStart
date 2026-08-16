using DevStart.Application.Abstractions.Captcha;
using DevStart.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DevStart.Infrastructure.Captcha
{
    /// <summary>
    /// Validates SmartCaptcha tokens against https://smartcaptcha.yandexcloud.net/validate, which
    /// answers {"status":"ok"|"failed","message":"..."} to a form-encoded secret/token/ip POST.
    /// </summary>
    internal sealed class YandexSmartCaptchaVerifier : ICaptchaVerifier
    {
        private readonly HttpClient _httpClient;
        private readonly CaptchaOptions _options;
        private readonly ILogger<YandexSmartCaptchaVerifier> _logger;

        public YandexSmartCaptchaVerifier(
            HttpClient httpClient,
            IOptions<CaptchaOptions> options,
            ILogger<YandexSmartCaptchaVerifier> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<CaptchaOutcome> VerifyAsync(
            string token,
            string? clientIp,
            CancellationToken cancellationToken)
        {
            var form = new Dictionary<string, string>
            {
                ["secret"] = _options.ServerKey,
                ["token"] = token,
            };

            if (!string.IsNullOrWhiteSpace(clientIp))
            {
                form["ip"] = clientIp;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _options.ValidateUrl)
                {
                    Content = new FormUrlEncodedContent(form),
                };

                using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

                // Deliberately NOT EnsureSuccessStatusCode() (unlike GitHubAuthProvider): a 5xx from the
                // captcha vendor must degrade into Unavailable, which the caller can fail open on, not
                // explode into a 500 for a user who is only trying to log in.
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "SmartCaptcha validate returned {StatusCode}; treating as unavailable.",
                        (int)response.StatusCode);
                    return CaptchaOutcome.Unavailable;
                }

                ValidateResponse? body = await response.Content
                    .ReadFromJsonAsync<ValidateResponse>(cancellationToken);

                if (body is null || string.IsNullOrEmpty(body.Status))
                {
                    _logger.LogWarning("SmartCaptcha validate returned an unparseable body.");
                    return CaptchaOutcome.Unavailable;
                }

                if (string.Equals(body.Status, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    return CaptchaOutcome.Human;
                }

                // Parsed cleanly and is not "ok" — an explicit rejection. Never log the token itself.
                _logger.LogInformation(
                    "SmartCaptcha rejected a token: {Status} {Message}", body.Status, body.Message);
                return CaptchaOutcome.Bot;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       && !cancellationToken.IsCancellationRequested)
            {
                // The guard matters: a caller disconnect cancels our token too, and that must surface as
                // cancellation rather than being misreported as a captcha outage. Same idiom as
                // YooKassaResilienceHandler.
                _logger.LogWarning(ex, "SmartCaptcha validate call failed; treating as unavailable.");
                return CaptchaOutcome.Unavailable;
            }
        }

        private sealed class ValidateResponse
        {
            [JsonPropertyName("status")] public string? Status { get; set; }
            [JsonPropertyName("message")] public string? Message { get; set; }
        }
    }
}
