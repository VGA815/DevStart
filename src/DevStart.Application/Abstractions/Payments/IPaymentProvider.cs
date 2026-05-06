using DevStart.Domain.Payments;

namespace DevStart.Application.Abstractions.Payments
{
    public sealed record CreatedPayment(string ProviderPaymentId, string ConfirmationUrl);

    public sealed record PaymentWebhookEvent(
        string ProviderPaymentId,
        PaymentStatus NewStatus,
        DateTime EventTime,
        bool ShouldProcess = true);

    public interface IPaymentProvider
    {
        /// <summary>
        /// Creates a payment in the external provider and returns the provider's payment id and the
        /// confirmation URL the user must be redirected to.
        /// </summary>
        Task<CreatedPayment> CreatePaymentAsync(
            decimal amount,
            string currency,
            string description,
            string returnUrl,
            string idempotenceKey,
            CancellationToken ct);

        /// <summary>
        /// Parses a provider webhook body. Returns null when the payload is malformed or unsupported.
        /// Caller is responsible for IP/signature verification before invoking this method.
        /// </summary>
        PaymentWebhookEvent? ParseWebhook(string body);
    }
}
