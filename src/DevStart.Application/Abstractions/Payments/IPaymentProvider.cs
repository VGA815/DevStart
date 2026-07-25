using DevStart.Domain.Payments;

namespace DevStart.Application.Abstractions.Payments
{
    public sealed record CreatedPayment(string ProviderPaymentId, string ConfirmationUrl);

    /// <summary>
    /// Result of creating a refund. <see cref="Succeeded"/> is <c>true</c> when the provider already
    /// reports the refund as completed, so the caller can reflect it locally immediately instead of
    /// waiting for the <c>refund.succeeded</c> webhook.
    /// </summary>
    public sealed record CreatedRefund(string RefundId, bool Succeeded);

    /// <summary>
    /// Input for creating a one-time payment. Carries the data required to register a 54-FZ/NPD
    /// receipt (<see cref="CustomerEmail"/>) and internal identifiers attached as provider metadata
    /// so a webhook or reconciliation pass can always be mapped back to our records.
    /// </summary>
    public sealed record CreatePaymentInput(
        decimal Amount,
        string Currency,
        string Description,
        string ReturnUrl,
        string IdempotenceKey,
        string CustomerEmail,
        Guid PaymentId,
        Guid UserId,
        Guid? SubscriptionId = null,
        Guid? ServiceOrderId = null);

    /// <summary>
    /// Input for refunding (fully or partially) a captured payment. A refund receipt is registered
    /// for the same customer so the NPD "возврат прихода" cheque is issued.
    /// </summary>
    public sealed record CreateRefundInput(
        string ProviderPaymentId,
        decimal Amount,
        string Currency,
        string Description,
        string CustomerEmail,
        string IdempotenceKey);

    /// <summary>
    /// Authoritative payment state read back from the provider via GET /payments/{id}. This — not the
    /// webhook body — is the source of truth used to transition our <see cref="Payment"/>.
    /// </summary>
    public sealed record ProviderPaymentSnapshot(
        string ProviderPaymentId,
        PaymentStatus Status,
        bool Paid,
        DateTime? PaidAt,
        decimal RefundedAmount,
        string? ReceiptRegistration);

    public enum WebhookEventKind
    {
        Unsupported = 0,
        PaymentSucceeded = 1,
        PaymentCanceled = 2,
        RefundSucceeded = 3,
    }

    /// <summary>
    /// Result of parsing a provider webhook body. The body is treated only as a trigger; the
    /// authoritative state is re-read via <see cref="IPaymentProvider.GetPaymentAsync"/>.
    /// <see cref="ProviderPaymentId"/> is always the payment id (for refund events it is the
    /// refund's <c>payment_id</c>).
    /// </summary>
    public sealed record PaymentWebhookEvent(WebhookEventKind Kind, string ProviderPaymentId);

    public interface IPaymentProvider
    {
        /// <summary>
        /// Creates a payment in the external provider and returns the provider's payment id and the
        /// confirmation URL the user must be redirected to.
        /// </summary>
        Task<CreatedPayment> CreatePaymentAsync(CreatePaymentInput input, CancellationToken ct);

        /// <summary>
        /// Reads the current authoritative state of a payment. Returns <c>null</c> when the payment
        /// cannot be retrieved (transient error / not yet visible) so the caller can retry later.
        /// </summary>
        Task<ProviderPaymentSnapshot?> GetPaymentAsync(string providerPaymentId, CancellationToken ct);

        /// <summary>
        /// Creates a refund for a captured payment and returns the provider's refund id together with
        /// whether the refund is already completed.
        /// </summary>
        Task<CreatedRefund> CreateRefundAsync(CreateRefundInput input, CancellationToken ct);

        /// <summary>
        /// Parses a provider webhook body. Returns <c>null</c> when the payload is malformed.
        /// Caller is responsible for IP/signature verification before invoking this method.
        /// </summary>
        PaymentWebhookEvent? ParseWebhook(string body);
    }
}
