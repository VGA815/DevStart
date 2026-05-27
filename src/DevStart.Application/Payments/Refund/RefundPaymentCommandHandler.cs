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
            decimal amount = command.Amount ?? refundableBalance;
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
                if (newTotal >= payment.Amount)
                {
                    Subscription? subscription = await context.Subscriptions
                        .SingleOrDefaultAsync(s => s.Id == payment.SubscriptionId, cancellationToken);
                    subscription?.MarkCancelled(utcNow);
                }
                await context.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }
    }
}
