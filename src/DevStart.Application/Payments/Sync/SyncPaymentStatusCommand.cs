using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Payments.Sync
{
    /// <summary>
    /// Re-reads a payment's authoritative state from the provider and transitions the local
    /// <c>Payment</c> and <c>Subscription</c> accordingly. Invoked both by the YooKassa webhook
    /// (as a trigger) and by the reconciliation job (for payments stuck in Pending). Idempotent.
    /// </summary>
    public sealed record SyncPaymentStatusCommand(string ProviderPaymentId) : ICommand;
}
