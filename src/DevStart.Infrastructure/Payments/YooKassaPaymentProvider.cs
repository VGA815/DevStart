using DevStart.Application.Abstractions.Payments;
using DevStart.Domain.Payments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevStart.Infrastructure.Payments
{
    /// <summary>
    /// YooKassa REST API integration. Authentication: HTTP Basic with shop_id:secret_key.
    /// Idempotence-Key header is required by YooKassa for POST /v3/payments.
    /// Webhook authentication is handled at the endpoint layer via IP allowlist.
    /// </summary>
    internal sealed class YooKassaPaymentProvider : IPaymentProvider
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly HttpClient _http;
        private readonly YooKassaOptions _options;
        private readonly ILogger<YooKassaPaymentProvider> _logger;

        public YooKassaPaymentProvider(
            HttpClient http,
            IOptions<YooKassaOptions> options,
            ILogger<YooKassaPaymentProvider> logger)
        {
            _options = options.Value;
            _logger = logger;

            _http = http;
            _http.BaseAddress = new Uri(_options.ApiUrl);
            string credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.ShopId}:{_options.SecretKey}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        public async Task<CreatedPayment> CreatePaymentAsync(
            decimal amount,
            string currency,
            string description,
            string returnUrl,
            string idempotenceKey,
            CancellationToken ct)
        {
            var payload = new YooKassaCreatePaymentRequest
            {
                Amount = new YooKassaMoney
                {
                    Value = amount.ToString("0.00", CultureInfo.InvariantCulture),
                    Currency = currency,
                },
                Capture = true,
                Confirmation = new YooKassaConfirmation
                {
                    Type = "redirect",
                    ReturnUrl = returnUrl,
                },
                Description = description,
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "/v3/payments")
            {
                Content = JsonContent.Create(payload, options: SerializerOptions),
            };
            request.Headers.Add("Idempotence-Key", idempotenceKey);

            using HttpResponseMessage response = await _http.SendAsync(request, ct);
            string body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "YooKassa create payment failed: {StatusCode} {Body}",
                    (int)response.StatusCode, body);
                throw new InvalidOperationException(
                    $"YooKassa returned {(int)response.StatusCode}: {body}");
            }

            YooKassaPaymentResponse? parsed = JsonSerializer.Deserialize<YooKassaPaymentResponse>(
                body, SerializerOptions);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Id) ||
                parsed.Confirmation is null ||
                string.IsNullOrWhiteSpace(parsed.Confirmation.ConfirmationUrl))
            {
                throw new InvalidOperationException(
                    $"YooKassa returned an unexpected payload: {body}");
            }

            return new CreatedPayment(parsed.Id, parsed.Confirmation.ConfirmationUrl);
        }

        public PaymentWebhookEvent? ParseWebhook(string body)
        {
            try
            {
                YooKassaWebhookPayload? payload = JsonSerializer.Deserialize<YooKassaWebhookPayload>(
                    body, SerializerOptions);
                if (payload?.Object is null || string.IsNullOrWhiteSpace(payload.Object.Id))
                {
                    return null;
                }

                if (payload.Event is not "payment.succeeded" and not "payment.canceled")
                {
                    return new PaymentWebhookEvent(
                        payload.Object.Id,
                        PaymentStatus.Pending,
                        DateTime.UtcNow,
                        ShouldProcess: false);
                }

                PaymentStatus? status = payload.Event switch
                {
                    "payment.succeeded" when payload.Object.Status == "succeeded" => PaymentStatus.Succeeded,
                    "payment.canceled" when payload.Object.Status == "canceled" => PaymentStatus.Cancelled,
                    _ => null,
                };

                if (status is null)
                {
                    return null;
                }

                DateTime eventTime = payload.Object.PaidAt
                    ?? payload.Object.CapturedAt
                    ?? payload.Object.CreatedAt
                    ?? DateTime.UtcNow;

                return new PaymentWebhookEvent(payload.Object.Id, status.Value, eventTime);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse YooKassa webhook body");
                return null;
            }
        }

        // --- DTOs for YooKassa REST ---

        private sealed class YooKassaCreatePaymentRequest
        {
            public YooKassaMoney Amount { get; set; } = null!;
            public bool Capture { get; set; }
            public YooKassaConfirmation Confirmation { get; set; } = null!;
            public string? Description { get; set; }
        }

        private sealed class YooKassaMoney
        {
            public string Value { get; set; } = null!;
            public string Currency { get; set; } = null!;
        }

        private sealed class YooKassaConfirmation
        {
            public string Type { get; set; } = null!;
            public string? ReturnUrl { get; set; }
            public string? ConfirmationUrl { get; set; }
        }

        private sealed class YooKassaPaymentResponse
        {
            public string? Id { get; set; }
            public string? Status { get; set; }
            public YooKassaConfirmation? Confirmation { get; set; }
        }

        private sealed class YooKassaWebhookPayload
        {
            public string? Event { get; set; }
            public YooKassaWebhookObject? Object { get; set; }
        }

        private sealed class YooKassaWebhookObject
        {
            public string? Id { get; set; }
            public string? Status { get; set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? CapturedAt { get; set; }
            public DateTime? PaidAt { get; set; }
        }
    }
}
