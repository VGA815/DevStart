using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Payments.Refund
{
    /// <summary>
    /// Initiates a refund for a captured payment. When <see cref="Proportional"/> is set the amount is
    /// computed from the unused subscription period (offer §6.2) and <see cref="Amount"/> is ignored;
    /// otherwise the refund is full when <see cref="Amount"/> is null, or partial for the given amount.
    /// When the provider reports the refund as already completed it is reflected locally immediately;
    /// otherwise the local <c>Payment</c>/<c>Subscription</c> are updated when the <c>refund.succeeded</c>
    /// webhook arrives or the reconciliation job re-syncs (both idempotent).
    /// </summary>
    public sealed record RefundPaymentCommand(Guid PaymentId, decimal? Amount, bool Proportional = false) : ICommand;
}
