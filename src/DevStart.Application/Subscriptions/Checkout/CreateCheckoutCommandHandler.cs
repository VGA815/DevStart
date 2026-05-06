using System.Linq;
using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Payments;
using DevStart.Domain.Payments;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.Application.Subscriptions.Checkout
{
    internal sealed class CreateCheckoutCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider,
        IPaymentProvider paymentProvider,
        IOptions<PlansOptions> plansOptions,
        IOptions<CheckoutOptions> checkoutOptions)
        : ICommandHandler<CreateCheckoutCommand, CheckoutResponse>
    {
        public async Task<Result<CheckoutResponse>> Handle(
            CreateCheckoutCommand command,
            CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;
            DateTime utcNow = dateTimeProvider.UtcNow;
            PlanOptions planConfig = plansOptions.Value.Pro;
            string returnUrl = checkoutOptions.Value.ReturnUrl;

            // 1. Already-active guard
            bool hasActive = await context.Subscriptions
                .AnyAsync(
                    s => s.UserId == userId
                      && s.Plan == SubscriptionPlan.Pro
                      && s.Status == SubscriptionStatus.Active
                      && s.ExpiresAt > utcNow,
                    cancellationToken);
            if (hasActive)
            {
                return Result.Failure<CheckoutResponse>(SubscriptionErrors.AlreadyActive);
            }

            // 2. Reuse existing Pending payment+subscription if user retries the checkout flow.
            Payment? pendingPayment = await context.Payments
                .Where(p => p.UserId == userId && p.Status == PaymentStatus.Pending)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            Subscription subscription;
            Payment payment;

            if (pendingPayment is not null)
            {
                Subscription? existing = await context.Subscriptions
                    .SingleOrDefaultAsync(s => s.Id == pendingPayment.SubscriptionId, cancellationToken);
                if (existing is null)
                {
                    return Result.Failure<CheckoutResponse>(
                        SubscriptionErrors.NotFound(pendingPayment.SubscriptionId));
                }
                subscription = existing;
                payment = pendingPayment;

                if (!string.IsNullOrWhiteSpace(payment.ProviderPaymentId)
                    && !string.IsNullOrWhiteSpace(payment.ConfirmationUrl))
                {
                    return new CheckoutResponse
                    {
                        SubscriptionId = subscription.Id,
                        PaymentId = payment.Id,
                        ConfirmationUrl = payment.ConfirmationUrl,
                    };
                }

                CreatedPayment created = await paymentProvider.CreatePaymentAsync(
                    amount: planConfig.Price,
                    currency: planConfig.Currency,
                    description: planConfig.Description,
                    returnUrl: returnUrl,
                    idempotenceKey: payment.Id.ToString(),
                    ct: cancellationToken);
                payment.AssignProviderPayment(created.ProviderPaymentId, created.ConfirmationUrl);
                await context.SaveChangesAsync(cancellationToken);
                return new CheckoutResponse
                {
                    SubscriptionId = subscription.Id,
                    PaymentId = payment.Id,
                    ConfirmationUrl = created.ConfirmationUrl,
                };
            }
            else
            {
                subscription = Subscription.CreatePending(userId, command.Plan, utcNow);
                payment = Payment.CreatePending(
                    userId,
                    subscription.Id,
                    PaymentProvider.YooKassa,
                    planConfig.Price,
                    planConfig.Currency,
                    utcNow);

                context.Subscriptions.Add(subscription);
                context.Payments.Add(payment);
                await context.SaveChangesAsync(cancellationToken);
            }

            async Task<CreatedPayment> CreateProviderPaymentAsync(Payment paymentToUse)
            {
                return await paymentProvider.CreatePaymentAsync(
                    amount: planConfig.Price,
                    currency: planConfig.Currency,
                    description: planConfig.Description,
                    returnUrl: returnUrl,
                    idempotenceKey: paymentToUse.Id.ToString(),
                    ct: cancellationToken);
            }

            CreatedPayment createdPayment = await CreateProviderPaymentAsync(payment);

            payment.AssignProviderPayment(createdPayment.ProviderPaymentId, createdPayment.ConfirmationUrl);
            await context.SaveChangesAsync(cancellationToken);

            return new CheckoutResponse
            {
                SubscriptionId = subscription.Id,
                PaymentId = payment.Id,
                ConfirmationUrl = createdPayment.ConfirmationUrl,
            };
        }
    }
}