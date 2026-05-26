using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Payments.Sync;
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
        IOptions<CheckoutOptions> checkoutOptions,
        ICommandHandler<SyncPaymentStatusCommand> syncHandler)
        : ICommandHandler<CreateCheckoutCommand, CheckoutResponse>
    {
        // A confirmation link older than this may have expired at the provider; re-confirm before reuse.
        private static readonly TimeSpan StaleCheckoutLinkAfter = TimeSpan.FromMinutes(30);

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

            // If a prior checkout link is old enough to have expired at the provider, re-confirm it
            // before reusing. If it actually succeeded, the user is now Pro — block a second charge.
            // If it was cancelled/expired, discard it so a fresh payment can be created below.
            if (pendingPayment is not null
                && !string.IsNullOrWhiteSpace(pendingPayment.ProviderPaymentId)
                && pendingPayment.CreatedAt < utcNow - StaleCheckoutLinkAfter)
            {
                await syncHandler.Handle(
                    new SyncPaymentStatusCommand(pendingPayment.ProviderPaymentId!), cancellationToken);
                if (pendingPayment.Status == PaymentStatus.Succeeded)
                {
                    return Result.Failure<CheckoutResponse>(SubscriptionErrors.AlreadyActive);
                }
                if (pendingPayment.Status != PaymentStatus.Pending)
                {
                    pendingPayment = null;
                }
            }

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
                try
                {
                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    // Lost a concurrent checkout race: the DB rejected our duplicate pending payment
                    // (unique index ux_payments_user_pending). Discard ours and reuse the winner so the
                    // user is never charged twice.
                    context.Payments.Remove(payment);
                    context.Subscriptions.Remove(subscription);

                    Payment? winner = await context.Payments
                        .Where(p => p.UserId == userId && p.Status == PaymentStatus.Pending)
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken);
                    Subscription? winnerSubscription = winner is null
                        ? null
                        : await context.Subscriptions
                            .SingleOrDefaultAsync(s => s.Id == winner.SubscriptionId, cancellationToken);
                    if (winner is null || winnerSubscription is null)
                    {
                        throw;
                    }

                    subscription = winnerSubscription;
                    payment = winner;
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
                    // Otherwise fall through and (idempotently) create the provider payment for the
                    // reused record — the IdempotenceKey is payment.Id, so YooKassa won't double-charge.
                }
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
