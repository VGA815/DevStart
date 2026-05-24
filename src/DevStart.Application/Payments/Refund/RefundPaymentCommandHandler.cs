using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Subscriptions;
using DevStart.Domain.Payments;
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

            string refundId = await paymentProvider.CreateRefundAsync(input, cancellationToken);

            logger.LogInformation(
                "Initiated YooKassa refund {RefundId} for payment {PaymentId} ({Amount} {Currency}).",
                refundId, payment.Id, amountKey, payment.Currency);

            // The local payment/subscription are updated when the refund.succeeded webhook arrives.
            return Result.Success();
        }
    }
}
