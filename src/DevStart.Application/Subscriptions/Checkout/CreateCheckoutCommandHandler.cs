using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Payments.Sync;
using DevStart.Domain.Payments;
using DevStart.Domain.PromoCodes;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevStart.Application.Subscriptions.Checkout
{
    internal sealed class CreateCheckoutCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider,
        IPaymentProvider paymentProvider,
        INpdIncomeService npdIncomeService,
        IOptions<PlansOptions> plansOptions,
        IOptions<CheckoutOptions> checkoutOptions,
        ICommandHandler<SyncPaymentStatusCommand> syncHandler,
        ILogger<CreateCheckoutCommandHandler> logger)
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

            // 1b. Resolve and validate a promo code (if any). A free/100%-off code activates Pro directly
            // without touching the payment provider; a partial discount lowers the amount charged below.
            decimal chargeAmount = planConfig.Price;
            Guid? promoCodeId = null;
            decimal discountAmount = 0m;
            if (!string.IsNullOrWhiteSpace(command.PromoCode))
            {
                string normalizedCode = PromoCode.Normalize(command.PromoCode);
                PromoCode? promo = await context.PromoCodes
                    .FirstOrDefaultAsync(p => p.Code == normalizedCode, cancellationToken);
                if (promo is null)
                {
                    return Result.Failure<CheckoutResponse>(PromoCodeErrors.InvalidCode);
                }

                bool alreadyRedeemed = await context.PromoCodeRedemptions
                    .AnyAsync(r => r.PromoCodeId == promo.Id && r.UserId == userId, cancellationToken);
                Result validation = promo.Validate(command.Plan, utcNow, alreadyRedeemed);
                if (validation.IsFailure)
                {
                    return Result.Failure<CheckoutResponse>(validation.Error);
                }

                PromoCheckout promoCheckout = promo.ComputeCheckout(planConfig.Price);

                if (promoCheckout.IsFree)
                {
                    Subscription freeSubscription = Subscription.CreatePending(
                        userId, command.Plan, utcNow, SubscriptionSource.Promo);
                    Result activated = freeSubscription.Activate(
                        utcNow, promoCheckout.FreeDays ?? planConfig.DurationDays);
                    if (activated.IsFailure)
                    {
                        return Result.Failure<CheckoutResponse>(activated.Error);
                    }

                    context.Subscriptions.Add(freeSubscription);
                    context.PromoCodeRedemptions.Add(PromoCodeRedemption.Create(
                        promo.Id, userId, freeSubscription.Id, paymentId: null, promoCheckout.Discount, utcNow));
                    promo.RegisterRedemption();

                    try
                    {
                        // The unique (promo_code_id, user_id) index is the final guard against a concurrent
                        // double-redemption that slips past the AnyAsync check above.
                        await context.SaveChangesAsync(cancellationToken);
                    }
                    catch (DbUpdateException)
                    {
                        return Result.Failure<CheckoutResponse>(PromoCodeErrors.AlreadyRedeemedByUser);
                    }

                    return new CheckoutResponse
                    {
                        SubscriptionId = freeSubscription.Id,
                        PaymentId = Guid.Empty,
                        ConfirmationUrl = null,
                        Activated = true,
                    };
                }

                chargeAmount = promoCheckout.Amount;
                promoCodeId = promo.Id;
                discountAmount = promoCheckout.Discount;
            }

            // 2. Reuse an existing Pending payment+subscription if the user retries checkout.
            Payment? pendingPayment = await context.Payments
                .Where(p => p.UserId == userId && p.Status == PaymentStatus.Pending
                         && p.Purpose == PaymentPurpose.Subscription)
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
                // A promo code was supplied but an in-flight checkout already exists with a different
                // (or no) promo. Don't silently reuse the old amount/discount — make the user finish or
                // cancel the pending checkout first.
                if (!string.IsNullOrWhiteSpace(command.PromoCode) && pendingPayment.PromoCodeId != promoCodeId)
                {
                    return Result.Failure<CheckoutResponse>(PaymentErrors.PendingCheckoutPromoMismatch);
                }

                Subscription? existing = await context.Subscriptions
                    .SingleOrDefaultAsync(s => s.Id == pendingPayment.SubscriptionId, cancellationToken);
                if (existing is null)
                {
                    return Result.Failure<CheckoutResponse>(
                        SubscriptionErrors.NotFound(pendingPayment.SubscriptionId.GetValueOrDefault()));
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
                // SC-42: hard stop — do not create a new paid operation once accepting this charge
                // would cross the self-employed (НПД) annual income cap.
                Result incomeGate = await npdIncomeService.EnsureCanAcceptPaymentAsync(
                    chargeAmount, cancellationToken);
                if (incomeGate.IsFailure)
                {
                    return Result.Failure<CheckoutResponse>(incomeGate.Error);
                }

                subscription = Subscription.CreatePending(userId, command.Plan, utcNow);
                payment = Payment.CreatePending(
                    userId,
                    subscription.Id,
                    PaymentProvider.YooKassa,
                    chargeAmount,
                    planConfig.Currency,
                    utcNow,
                    promoCodeId,
                    discountAmount);

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
                        .Where(p => p.UserId == userId && p.Status == PaymentStatus.Pending
                         && p.Purpose == PaymentPurpose.Subscription)
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
            // Use the payment's own amount: it reflects any promo discount applied when it was created.
            var input = new CreatePaymentInput(
                Amount: payment.Amount,
                Currency: planConfig.Currency,
                Description: planConfig.Description,
                ReturnUrl: returnUrl,
                IdempotenceKey: payment.Id.ToString(),
                CustomerEmail: customerEmail,
                PaymentId: payment.Id,
                SubscriptionId: subscription.Id,
                UserId: userId);

            CreatedPayment created;
            try
            {
                created = await paymentProvider.CreatePaymentAsync(input, cancellationToken);
            }
            catch (PaymentProviderException ex)
            {
                // The pending payment/subscription stays persisted; its id is the idempotence key, so a
                // retry reuses it and never double-charges. Surface a clean 503/400 instead of a 500.
                logger.LogError(ex, "Failed to create YooKassa payment for payment {PaymentId}", payment.Id);
                return Result.Failure<CheckoutResponse>(
                    ex.IsTransient
                        ? PaymentErrors.ProviderUnavailable(ex.Message)
                        : PaymentErrors.ProviderError(ex.Message));
            }

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
