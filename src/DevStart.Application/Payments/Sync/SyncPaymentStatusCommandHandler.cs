using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Subscriptions;
using DevStart.Domain.Payments;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevStart.Application.Payments.Sync
{
    internal sealed class SyncPaymentStatusCommandHandler(
        IApplicationDbContext context,
        IPaymentProvider paymentProvider,
        IDateTimeProvider dateTimeProvider,
        IOptions<PlansOptions> plansOptions,
        ILogger<SyncPaymentStatusCommandHandler> logger)
        : ICommandHandler<SyncPaymentStatusCommand>
    {
        public async Task<Result> Handle(SyncPaymentStatusCommand command, CancellationToken cancellationToken)
        {
            ProviderPaymentSnapshot? snapshot =
                await paymentProvider.GetPaymentAsync(command.ProviderPaymentId, cancellationToken);
            if (snapshot is null)
            {
                // Transient: cannot confirm right now. The reconciliation job will retry.
                logger.LogWarning(
                    "Could not read YooKassa payment {ProviderPaymentId}; will retry later.",
                    command.ProviderPaymentId);
                return Result.Success();
            }

            Payment? payment = await context.Payments
                .SingleOrDefaultAsync(
                    p => p.Provider == PaymentProvider.YooKassa
                      && p.ProviderPaymentId == command.ProviderPaymentId,
                    cancellationToken);
            if (payment is null)
            {
                logger.LogWarning(
                    "YooKassa payment {ProviderPaymentId} has no local record.",
                    command.ProviderPaymentId);
                return Result.Failure(PaymentErrors.NotFoundByProviderId(command.ProviderPaymentId));
            }

            Subscription? subscription = await context.Subscriptions
                .SingleOrDefaultAsync(s => s.Id == payment.SubscriptionId, cancellationToken);

            DateTime utcNow = dateTimeProvider.UtcNow;
            bool fullyRefunded = snapshot.RefundedAmount > 0m && snapshot.RefundedAmount >= payment.Amount;

            if (fullyRefunded)
            {
                if (payment.Status == PaymentStatus.Pending && snapshot.Paid)
                {
                    payment.MarkSucceeded(snapshot.PaidAt ?? utcNow);
                }
                payment.MarkRefunded(snapshot.RefundedAmount, utcNow);
                subscription?.MarkCancelled(utcNow);
            }
            else if (snapshot.Status == PaymentStatus.Succeeded)
            {
                Result paid = payment.MarkSucceeded(snapshot.PaidAt ?? utcNow);
                if (paid.IsFailure)
                {
                    return paid;
                }
                if (subscription is null)
                {
                    return Result.Failure(SubscriptionErrors.NotFound(payment.SubscriptionId));
                }
                if (subscription.Status == SubscriptionStatus.Pending)
                {
                    Result activated = subscription.Activate(utcNow, plansOptions.Value.Pro.DurationDays);
                    if (activated.IsFailure)
                    {
                        return activated;
                    }
                }
                if (snapshot.RefundedAmount > 0m)
                {
                    // Partial refund: record the amount but keep access.
                    payment.MarkRefunded(snapshot.RefundedAmount, utcNow);
                }
            }
            else if (snapshot.Status == PaymentStatus.Cancelled)
            {
                payment.MarkCancelled(utcNow);
                subscription?.MarkCancelled(utcNow);
            }
            // Pending → nothing to do yet.

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
