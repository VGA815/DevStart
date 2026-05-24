using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Payments.Sync;
using DevStart.Domain.Payments;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevStart.Infrastructure.Payments
{
    /// <summary>
    /// Recurring Hangfire job that resolves payments stuck in <see cref="PaymentStatus.Pending"/>
    /// because their webhook was missed. For each stale payment it re-reads the authoritative state
    /// from YooKassa via <see cref="SyncPaymentStatusCommand"/>. Idempotent and safe to run often.
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

            List<string> providerPaymentIds = await context.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Pending
                         && p.ProviderPaymentId != null
                         && p.CreatedAt <= newestEligible
                         && p.CreatedAt >= oldestEligible)
                .Select(p => p.ProviderPaymentId!)
                .ToListAsync(cancellationToken);

            if (providerPaymentIds.Count == 0)
            {
                return;
            }

            logger.LogInformation(
                "Reconciling {Count} pending YooKassa payment(s).", providerPaymentIds.Count);

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
    }
}
