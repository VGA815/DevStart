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

        public static Error ProviderUnavailable(string message) => Error.ServiceUnavailable(
            "Payments.ProviderUnavailable",
            $"The payment provider is temporarily unavailable: {message}");

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

        public static readonly Error PendingCheckoutPromoMismatch = Error.Conflict(
            "Payments.PendingCheckoutPromoMismatch",
            "You already have a pending checkout with a different (or no) promo code. " +
            "Complete or cancel it before applying this promo code.");

        public static Error RefundAmountInvalid(decimal max) => Error.Problem(
            "Payments.RefundAmountInvalid",
            $"Refund amount must be greater than zero and not exceed the refundable balance ({max}).");

        // A proportional refund prorates the unused part of a subscription period. A one-time service
        // has no period to prorate, so the caller has to name an amount (or refund in full).
        public static readonly Error ProportionalNotApplicable = Error.Problem(
            "Payments.ProportionalNotApplicable",
            "A proportional refund only applies to a subscription payment. " +
            "Refund a one-time service order in full or specify an amount.");

        // Hard stop for the self-employed (НПД, ФЗ-422) annual income cap: once the calendar-year net
        // income reaches the limit, no new paid operation may be created until the next year.
        public static readonly Error IncomeLimitReached = Error.Conflict(
            "Payments.IncomeLimitReached",
            "The annual self-employed (НПД) income limit has been reached. " +
            "No new paid operations can be created until the next calendar year.");
    }
}
