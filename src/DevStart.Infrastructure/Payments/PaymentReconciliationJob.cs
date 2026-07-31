using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Payments.Sync;
using DevStart.Domain.Payments;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevStart.Infrastructure.Payments
{
    /// <summary>
    /// Recurring Hangfire job that keeps payment state in sync with the provider. It (1) re-reads
    /// <see cref="PaymentStatus.Pending"/> payments whose webhook was missed, (2) re-reads captured
    /// payments that may have been refunded (missed/out-of-band <c>refund.succeeded</c>), and
    /// (3) cancels payments abandoned past the reconcile window so they stop blocking new checkouts.
    /// All work goes through <see cref="SyncPaymentStatusCommand"/> and is idempotent / safe to run often.
    /// </summary>
    public sealed class PaymentReconciliationJob(
        IApplicationDbContext context,
        ICommandHandler<SyncPaymentStatusCommand> syncHandler,
        IDateTimeProvider dateTimeProvider,
        IOptions<BillingMaintenanceOptions> options,
        ILogger<PaymentReconciliationJob> logger)
    {
        public async Task ReconcilePendingAsync(CancellationToken cancellationToken)
        {
            BillingMaintenanceOptions opts = options.Value;
            DateTime now = dateTimeProvider.UtcNow;
            DateTime newestEligible = now.AddMinutes(-opts.ReconcileMinAgeMinutes);
            DateTime oldestEligible = now.AddHours(-opts.ReconcileMaxAgeHours);
            DateTime refundCutoff = now.AddHours(-opts.RefundReconcileWindowHours);

            List<string> pendingIds = await context.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Pending
                         && p.ProviderPaymentId != null
                         && p.CreatedAt <= newestEligible
                         && p.CreatedAt >= oldestEligible)
                .Select(p => p.ProviderPaymentId!)
                .ToListAsync(cancellationToken);

            // Captured payments whose refund webhook may have been missed (or that were refunded
            // out-of-band in the provider dashboard). SyncPaymentStatusCommand re-reads refunded_amount
            // and is idempotent, so re-syncing these costs nothing when nothing changed.
            List<string> refundCandidateIds = await context.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Succeeded
                         && p.ProviderPaymentId != null
                         && p.RefundedAmount < p.Amount
                         && p.PaidAt != null
                         && p.PaidAt >= refundCutoff)
                .Select(p => p.ProviderPaymentId!)
                .ToListAsync(cancellationToken);

            List<string> providerPaymentIds = pendingIds.Union(refundCandidateIds).ToList();

            if (providerPaymentIds.Count > 0)
            {
                logger.LogInformation(
                    "Reconciling {Count} YooKassa payment(s): {Pending} pending, {Refund} refund-candidate.",
                    providerPaymentIds.Count, pendingIds.Count, refundCandidateIds.Count);

                foreach (string providerPaymentId in providerPaymentIds)
                {
                    try
                    {
                        Result result = await syncHandler.Handle(
                            new SyncPaymentStatusCommand(providerPaymentId), cancellationToken);
                        if (result.IsFailure)
                        {
                            logger.LogWarning(
                                "Reconciliation of payment {ProviderPaymentId} failed: {ErrorCode}",
                                providerPaymentId, result.Error.Code);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Reconciliation of payment {ProviderPaymentId} threw.", providerPaymentId);
                    }
                }
            }

            await CancelAbandonedPendingAsync(oldestEligible, cancellationToken);
        }

        /// <summary>
        /// Cancels payments that have been Pending past the reconcile window (abandoned checkouts).
        /// A final authoritative sync is attempted first so a late success is never discarded; whatever
        /// is still Pending afterwards is cancelled (with its subscription), freeing the single-pending
        /// slot and retiring its dead confirmation link.
        /// </summary>
        private async Task CancelAbandonedPendingAsync(DateTime oldestEligible, CancellationToken cancellationToken)
        {
            List<string> abandonedWithProvider = await context.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Pending
                         && p.ProviderPaymentId != null
                         && p.CreatedAt < oldestEligible)
                .Select(p => p.ProviderPaymentId!)
                .ToListAsync(cancellationToken);

            foreach (string providerPaymentId in abandonedWithProvider)
            {
                try
                {
                    await syncHandler.Handle(new SyncPaymentStatusCommand(providerPaymentId), cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Final sync of abandoned payment {ProviderPaymentId} threw.", providerPaymentId);
                }
            }

            List<Payment> stillPending = await context.Payments
                .Where(p => p.Status == PaymentStatus.Pending && p.CreatedAt < oldestEligible)
                .ToListAsync(cancellationToken);
            if (stillPending.Count == 0)
            {
                return;
            }

            List<Guid> subscriptionIds = stillPending
                .Where(p => p.SubscriptionId.HasValue)
                .Select(p => p.SubscriptionId!.Value)
                .Distinct()
                .ToList();
            List<Subscription> subscriptions = await context.Subscriptions
                .Where(s => subscriptionIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            // Abandoned one-time service orders have to be retired alongside their payment; otherwise
            // they sit Pending forever and clutter the buyer's order list.
            List<Guid> serviceOrderIds = stillPending
                .Where(p => p.ServiceOrderId.HasValue)
                .Select(p => p.ServiceOrderId!.Value)
                .Distinct()
                .ToList();
            List<ServiceOrder> serviceOrders = await context.ServiceOrders
                .Where(o => serviceOrderIds.Contains(o.Id))
                .ToListAsync(cancellationToken);

            DateTime now = dateTimeProvider.UtcNow;
            foreach (Payment payment in stillPending)
            {
                payment.MarkCancelled(now);
                subscriptions.FirstOrDefault(s => s.Id == payment.SubscriptionId)?.MarkCancelled(now);
                serviceOrders.FirstOrDefault(o => o.Id == payment.ServiceOrderId)?.MarkCancelled(now);
            }
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Cancelled {Count} abandoned pending payment(s).", stillPending.Count);
        }
    }
}
