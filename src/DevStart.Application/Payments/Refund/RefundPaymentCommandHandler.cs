using DevStart.Application.Abstractions.Caching;
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
using System.Globalization;

namespace DevStart.Application.Payments.Refund
{
    internal sealed class RefundPaymentCommandHandler(
        IApplicationDbContext context,
        IPaymentProvider paymentProvider,
        IDateTimeProvider dateTimeProvider,
        IOptions<PlansOptions> plansOptions,
        ICacheService cacheService,
        ILogger<RefundPaymentCommandHandler> logger)
        : ICommandHandler<RefundPaymentCommand>
    {
        public async Task<Result> Handle(RefundPaymentCommand command, CancellationToken cancellationToken)
        {
            Payment? payment = await context.Payments
                .SingleOrDefaultAsync(p => p.Id == command.PaymentId, cancellationToken);
            if (payment is null)
            {
                return Result.Failure(PaymentErrors.NotFound(command.PaymentId));
            }

            if (payment.Status != PaymentStatus.Succeeded || string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
            {
                return Result.Failure(PaymentErrors.NotRefundable);
            }

            decimal refundableBalance = payment.Amount - payment.RefundedAmount;

            // SC-48: a proportional refund returns the unused part of the subscription period
            // (offer §6.2): refund = paid × remaining/total, capped at the refundable balance.
            Subscription? subscription = null;
            decimal amount;
            if (command.Proportional)
            {
                subscription = await context.Subscriptions
                    .SingleOrDefaultAsync(s => s.Id == payment.SubscriptionId, cancellationToken);

                int totalDays = plansOptions.Value.Pro.DurationDays;
                double remainingDays = subscription is null
                    ? 0d
                    : Math.Max(0d, (subscription.ExpiresAt - dateTimeProvider.UtcNow).TotalDays);
                decimal fraction = totalDays <= 0
                    ? 0m
                    : (decimal)Math.Min(1d, remainingDays / totalDays);
                amount = Math.Min(
                    refundableBalance,
                    Math.Round(payment.Amount * fraction, 2, MidpointRounding.AwayFromZero));
            }
            else
            {
                amount = command.Amount ?? refundableBalance;
            }

            if (amount <= 0m || amount > refundableBalance)
            {
                return Result.Failure(PaymentErrors.RefundAmountInvalid(refundableBalance));
            }

            string? customerEmail = await context.Users
                .Where(u => u.Id == payment.UserId)
                .Select(u => u.Email)
                .SingleOrDefaultAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                return Result.Failure(PaymentErrors.CustomerEmailMissing);
            }

            string amountKey = amount.ToString("0.00", CultureInfo.InvariantCulture);
            var input = new CreateRefundInput(
                ProviderPaymentId: payment.ProviderPaymentId!,
                Amount: amount,
                Currency: payment.Currency,
                Description: $"Возврат — {plansOptions.Value.Pro.Description}",
                CustomerEmail: customerEmail,
                IdempotenceKey: $"refund:{payment.Id}:{amountKey}");

            CreatedRefund refund;
            try
            {
                refund = await paymentProvider.CreateRefundAsync(input, cancellationToken);
            }
            catch (PaymentProviderException ex)
            {
                logger.LogError(ex, "Failed to create YooKassa refund for payment {PaymentId}", payment.Id);
                return Result.Failure(
                    ex.IsTransient
                        ? PaymentErrors.ProviderUnavailable(ex.Message)
                        : PaymentErrors.ProviderError(ex.Message));
            }

            logger.LogInformation(
                "Initiated YooKassa refund {RefundId} for payment {PaymentId} ({Amount} {Currency}).",
                refund.RefundId, payment.Id, amountKey, payment.Currency);

            // When the provider already reports the refund as completed, reflect it locally now instead
            // of relying solely on the refund.succeeded webhook (which may be missed). The webhook /
            // reconciliation pass re-reads the authoritative refunded amount and is idempotent with this.
            if (refund.Succeeded)
            {
                DateTime utcNow = dateTimeProvider.UtcNow;
                decimal newTotal = payment.RefundedAmount + amount;
                payment.MarkRefunded(newTotal, utcNow);

                // A full refund cancels access; a proportional refund also ends it (the remaining
                // period has been paid back). MarkRefunded only raises the domain event on a *full*
                // refund, so clear the active-Pro cache here to cover the proportional (partial) case.
                bool endsAccess = newTotal >= payment.Amount || command.Proportional;
                if (endsAccess)
                {
                    subscription ??= await context.Subscriptions
                        .SingleOrDefaultAsync(s => s.Id == payment.SubscriptionId, cancellationToken);
                    subscription?.MarkCancelled(utcNow);
                    await cacheService.RemoveAsync(
                        CacheKeys.SubscriptionActiveByUser(payment.UserId), cancellationToken);
                }
                await context.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }
    }
}
