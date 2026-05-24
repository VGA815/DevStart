using DevStart.SharedKernel;

namespace DevStart.Domain.Payments
{
    public static class PaymentErrors
    {
        public static Error NotFound(Guid paymentId) => Error.NotFound(
            "Payments.NotFound",
            $"The payment with id = '{paymentId}' was not found.");

        public static Error NotFoundByProviderId(string providerPaymentId) => Error.NotFound(
            "Payments.NotFoundByProviderId",
            $"The payment with provider id = '{providerPaymentId}' was not found.");

        public static Error ProviderError(string message) => Error.Problem(
            "Payments.ProviderError",
            $"Payment provider returned an error: {message}");

        public static readonly Error WebhookSignatureInvalid = Error.Problem(
            "Payments.WebhookSignatureInvalid",
            "Webhook payload signature or origin is invalid.");

        public static readonly Error WebhookPayloadInvalid = Error.Problem(
            "Payments.WebhookPayloadInvalid",
            "Webhook payload could not be parsed.");

        public static readonly Error IdempotencyConflict = Error.Conflict(
            "Payments.IdempotencyConflict",
            "Payment with the same idempotency key already exists.");

        public static readonly Error CustomerEmailMissing = Error.Problem(
            "Payments.CustomerEmailMissing",
            "A customer email is required to issue a receipt (54-FZ/NPD). The account has no email address.");

        public static readonly Error NotRefundable = Error.Conflict(
            "Payments.NotRefundable",
            "Only a succeeded payment can be refunded.");

        public static Error RefundAmountInvalid(decimal max) => Error.Problem(
            "Payments.RefundAmountInvalid",
            $"Refund amount must be greater than zero and not exceed the refundable balance ({max}).");
    }
}
