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
    /// Idempotence-Key header is required by YooKassa for POST /v3/payments and /v3/refunds.
    /// Webhook authentication is handled at the endpoint layer via IP allowlist; payment state is
    /// always re-confirmed via GET /v3/payments/{id} rather than trusting the webhook body.
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
        private readonly YooKassaReceiptOptions _receiptOptions;
        private readonly ILogger<YooKassaPaymentProvider> _logger;

        public YooKassaPaymentProvider(
            HttpClient http,
            IOptions<YooKassaOptions> options,
            IOptions<YooKassaReceiptOptions> receiptOptions,
            ILogger<YooKassaPaymentProvider> logger)
        {
            _options = options.Value;
            _receiptOptions = receiptOptions.Value;
            _logger = logger;

            _http = http;
            _http.BaseAddress = new Uri(_options.ApiUrl);
            string credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.ShopId}:{_options.SecretKey}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        public async Task<CreatedPayment> CreatePaymentAsync(CreatePaymentInput input, CancellationToken ct)
        {
            var payload = new YooKassaCreatePaymentRequest
            {
                Amount = Money(input.Amount, input.Currency),
                Capture = true,
                Confirmation = new YooKassaConfirmationRequest
                {
                    Type = "redirect",
                    ReturnUrl = input.ReturnUrl,
                },
                Description = Trim(input.Description, 128),
                Receipt = BuildReceipt(input.CustomerEmail, input.Description, input.Amount, input.Currency),
                Metadata = new Dictionary<string, string>
                {
                    ["payment_id"] = input.PaymentId.ToString(),
                    ["subscription_id"] = input.SubscriptionId.ToString(),
                    ["user_id"] = input.UserId.ToString(),
                },
            };

            YooKassaPaymentResponse parsed = await SendAsync<YooKassaPaymentResponse>(
                HttpMethod.Post, "/v3/payments", payload, input.IdempotenceKey, ct);

            if (string.IsNullOrWhiteSpace(parsed.Id) ||
                parsed.Confirmation is null ||
                string.IsNullOrWhiteSpace(parsed.Confirmation.ConfirmationUrl))
            {
                throw new InvalidOperationException("YooKassa returned a payment without a confirmation URL.");
            }

            return new CreatedPayment(parsed.Id, parsed.Confirmation.ConfirmationUrl);
        }

        public async Task<ProviderPaymentSnapshot?> GetPaymentAsync(string providerPaymentId, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/v3/payments/{providerPaymentId}");
            using HttpResponseMessage response = await _http.SendAsync(request, ct);
            string body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "YooKassa get payment {ProviderPaymentId} failed: {StatusCode} {Body}",
                    providerPaymentId, (int)response.StatusCode, body);
                return null;
            }

            YooKassaPaymentResponse? parsed = JsonSerializer.Deserialize<YooKassaPaymentResponse>(body, SerializerOptions);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Id))
            {
                return null;
            }

            DateTime? paidAt = parsed.CapturedAt ?? parsed.CreatedAt;
            decimal refunded = ParseAmount(parsed.RefundedAmount?.Value);

            return new ProviderPaymentSnapshot(
                parsed.Id,
                MapStatus(parsed.Status),
                parsed.Paid,
                paidAt,
                refunded,
                parsed.ReceiptRegistration);
        }

        public async Task<CreatedRefund> CreateRefundAsync(CreateRefundInput input, CancellationToken ct)
        {
            var payload = new YooKassaCreateRefundRequest
            {
                PaymentId = input.ProviderPaymentId,
                Amount = Money(input.Amount, input.Currency),
                Receipt = BuildReceipt(input.CustomerEmail, input.Description, input.Amount, input.Currency),
            };

            YooKassaRefundResponse parsed = await SendAsync<YooKassaRefundResponse>(
                HttpMethod.Post, "/v3/refunds", payload, input.IdempotenceKey, ct);

            if (string.IsNullOrWhiteSpace(parsed.Id))
            {
                throw new InvalidOperationException("YooKassa returned a refund without an id.");
            }

            return new CreatedRefund(parsed.Id, parsed.Status == "succeeded");
        }

        public PaymentWebhookEvent? ParseWebhook(string body)
        {
            try
            {
                YooKassaWebhookPayload? payload = JsonSerializer.Deserialize<YooKassaWebhookPayload>(body, SerializerOptions);
                if (payload?.Object is null)
                {
                    return null;
                }

                switch (payload.Event)
                {
                    case "payment.succeeded" when !string.IsNullOrWhiteSpace(payload.Object.Id):
                        return new PaymentWebhookEvent(WebhookEventKind.PaymentSucceeded, payload.Object.Id!);
                    case "payment.canceled" when !string.IsNullOrWhiteSpace(payload.Object.Id):
                        return new PaymentWebhookEvent(WebhookEventKind.PaymentCanceled, payload.Object.Id!);
                    case "refund.succeeded":
                        // For refund events the webhook object is a Refund; the payment is in payment_id.
                        string? paymentId = payload.Object.PaymentId ?? payload.Object.Id;
                        if (string.IsNullOrWhiteSpace(paymentId))
                        {
                            return null;
                        }
                        return new PaymentWebhookEvent(WebhookEventKind.RefundSucceeded, paymentId!);
                    default:
                        return new PaymentWebhookEvent(
                            WebhookEventKind.Unsupported,
                            payload.Object.Id ?? string.Empty);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse YooKassa webhook body");
                return null;
            }
        }

        private YooKassaReceipt? BuildReceipt(string customerEmail, string description, decimal amount, string currency)
        {
            if (!_receiptOptions.Enabled || string.IsNullOrWhiteSpace(customerEmail))
            {
                return null;
            }

            return new YooKassaReceipt
            {
                Customer = new YooKassaReceiptCustomer { Email = customerEmail },
                Items =
                [
                    new YooKassaReceiptItem
                    {
                        Description = Trim(description, 128),
                        Quantity = "1.00",
                        Amount = Money(amount, currency),
                        VatCode = _receiptOptions.VatCode,
                        PaymentSubject = _receiptOptions.PaymentSubject,
                        PaymentMode = _receiptOptions.PaymentMode,
                    },
                ],
                TaxSystemCode = _receiptOptions.TaxSystemCode,
            };
        }

        private async Task<T> SendAsync<T>(
            HttpMethod method,
            string path,
            object payload,
            string idempotenceKey,
            CancellationToken ct)
        {
            using var request = new HttpRequestMessage(method, path)
            {
                // Serialize by the runtime type — `payload` is statically `object`, so the generic
                // JsonContent.Create<object> overload would emit an empty body.
                Content = JsonContent.Create(payload, payload.GetType(), options: SerializerOptions),
            };
            request.Headers.Add("Idempotence-Key", idempotenceKey);

            using HttpResponseMessage response = await _http.SendAsync(request, ct);
            string body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "YooKassa {Method} {Path} failed: {StatusCode} {Body}",
                    method, path, (int)response.StatusCode, body);
                throw new InvalidOperationException($"YooKassa returned {(int)response.StatusCode}: {body}");
            }

            T? parsed = JsonSerializer.Deserialize<T>(body, SerializerOptions);
            if (parsed is null)
            {
                throw new InvalidOperationException($"YooKassa returned an unexpected payload: {body}");
            }
            return parsed;
        }

        private static YooKassaMoney Money(decimal amount, string currency) => new()
        {
            Value = amount.ToString("0.00", CultureInfo.InvariantCulture),
            Currency = currency,
        };

        private static decimal ParseAmount(string? value) =>
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed)
                ? parsed
                : 0m;

        private static string Trim(string value, int maxLength) =>
            string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];

        private static PaymentStatus MapStatus(string? status) => status switch
        {
            "succeeded" => PaymentStatus.Succeeded,
            "canceled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending,
        };

        // --- DTOs for YooKassa REST ---

        private sealed class YooKassaCreatePaymentRequest
        {
            public YooKassaMoney Amount { get; set; } = null!;
            public bool Capture { get; set; }
            public YooKassaConfirmationRequest Confirmation { get; set; } = null!;
            public string? Description { get; set; }
            public YooKassaReceipt? Receipt { get; set; }
            public Dictionary<string, string>? Metadata { get; set; }
        }

        private sealed class YooKassaCreateRefundRequest
        {
            public string PaymentId { get; set; } = null!;
            public YooKassaMoney Amount { get; set; } = null!;
            public YooKassaReceipt? Receipt { get; set; }
        }

        private sealed class YooKassaMoney
        {
            public string Value { get; set; } = null!;
            public string Currency { get; set; } = null!;
        }

        private sealed class YooKassaConfirmationRequest
        {
            public string Type { get; set; } = null!;
            public string? ReturnUrl { get; set; }
        }

        private sealed class YooKassaReceipt
        {
            public YooKassaReceiptCustomer Customer { get; set; } = null!;
            public List<YooKassaReceiptItem> Items { get; set; } = [];
            public int? TaxSystemCode { get; set; }
        }

        private sealed class YooKassaReceiptCustomer
        {
            public string Email { get; set; } = null!;
        }

        private sealed class YooKassaReceiptItem
        {
            public string Description { get; set; } = null!;
            public string Quantity { get; set; } = null!;
            public YooKassaMoney Amount { get; set; } = null!;
            public int VatCode { get; set; }
            public string PaymentSubject { get; set; } = null!;
            public string PaymentMode { get; set; } = null!;
        }

        private sealed class YooKassaPaymentResponse
        {
            public string? Id { get; set; }
            public string? Status { get; set; }
            public bool Paid { get; set; }
            public YooKassaMoney? RefundedAmount { get; set; }
            public string? ReceiptRegistration { get; set; }
            public DateTime? CapturedAt { get; set; }
            public DateTime? CreatedAt { get; set; }
            public YooKassaConfirmationResponse? Confirmation { get; set; }
        }

        private sealed class YooKassaConfirmationResponse
        {
            public string? ConfirmationUrl { get; set; }
        }

        private sealed class YooKassaRefundResponse
        {
            public string? Id { get; set; }
            public string? Status { get; set; }
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
            public string? PaymentId { get; set; }
        }
    }
}
