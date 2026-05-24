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

            // The customer email is required to register the 54-FZ/NPD receipt ("Чеки от ЮKassa").
            string? customerEmail = await context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .SingleOrDefaultAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                return Result.Failure<CheckoutResponse>(PaymentErrors.CustomerEmailMissing);
            }

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

            // 2. Reuse an existing Pending payment+subscription if the user retries checkout.
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

                // The provider payment was already created on a previous attempt — reuse it.
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

            // The payment id doubles as the idempotence key so retries never create a duplicate
            // charge in YooKassa.
            var input = new CreatePaymentInput(
                Amount: planConfig.Price,
                Currency: planConfig.Currency,
                Description: planConfig.Description,
                ReturnUrl: returnUrl,
                IdempotenceKey: payment.Id.ToString(),
                CustomerEmail: customerEmail,
                PaymentId: payment.Id,
                SubscriptionId: subscription.Id,
                UserId: userId);

            CreatedPayment created = await paymentProvider.CreatePaymentAsync(input, cancellationToken);

            payment.AssignProviderPayment(created.ProviderPaymentId, created.ConfirmationUrl);
            await context.SaveChangesAsync(cancellationToken);

            return new CheckoutResponse
            {
                SubscriptionId = subscription.Id,
                PaymentId = payment.Id,
                ConfirmationUrl = created.ConfirmationUrl,
            };
        }
    }
}
