using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Payments.Refund
{
    /// <summary>
    /// Initiates a refund (full when <see cref="Amount"/> is null, otherwise partial) for a captured
    /// payment. When the provider reports the refund as already completed it is reflected locally
    /// immediately; otherwise the local <c>Payment</c>/<c>Subscription</c> are updated when the
    /// <c>refund.succeeded</c> webhook arrives or the reconciliation job re-syncs (both idempotent).
    /// </summary>
    public sealed record RefundPaymentCommand(Guid PaymentId, decimal? Amount) : ICommand;
}
