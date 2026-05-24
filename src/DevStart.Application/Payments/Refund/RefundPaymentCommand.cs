using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Payments.Refund
{
    /// <summary>
    /// Initiates a refund (full when <see cref="Amount"/> is null, otherwise partial) for a captured
    /// payment. The local <c>Payment</c>/<c>Subscription</c> are updated when the provider sends the
    /// <c>refund.succeeded</c> webhook (handled via <c>SyncPaymentStatusCommand</c>).
    /// </summary>
    public sealed record RefundPaymentCommand(Guid PaymentId, decimal? Amount) : ICommand;
}
