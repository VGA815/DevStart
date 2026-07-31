using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Subscriptions;
using DevStart.Domain.Payments;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevStart.Application.ServiceOrders.Checkout
{
    internal sealed class CreateServiceOrderCheckoutCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider,
        IPaymentProvider paymentProvider,
        INpdIncomeService npdIncomeService,
        IOptions<ServiceCatalogOptions> catalogOptions,
        IOptions<CheckoutOptions> checkoutOptions,
        ILogger<CreateServiceOrderCheckoutCommandHandler> logger)
        : ICommandHandler<CreateServiceOrderCheckoutCommand, ServiceOrderCheckoutResponse>
    {
        public async Task<Result<ServiceOrderCheckoutResponse>> Handle(
            CreateServiceOrderCheckoutCommand command,
            CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;
            DateTime utcNow = dateTimeProvider.UtcNow;

            ServiceCatalogItem? item = catalogOptions.Value.Find(command.ServiceType);
            if (item is null || item.Price <= 0m)
            {
                return Result.Failure<ServiceOrderCheckoutResponse>(
                    ServiceOrderErrors.UnknownServiceType(command.ServiceType.ToString()));
            }

            // The target is settled before anything is persisted and before the НПД counter is touched,
            // so a purchase that could never be delivered never reaches the payment provider.
            Result target = await ValidateTargetAsync(command, userId, utcNow, cancellationToken);
            if (target.IsFailure)
            {
                return Result.Failure<ServiceOrderCheckoutResponse>(target.Error);
            }

            // The customer email is required to register the 54-FZ/NPD receipt.
            string? customerEmail = await context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .SingleOrDefaultAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                return Result.Failure<ServiceOrderCheckoutResponse>(PaymentErrors.CustomerEmailMissing);
            }

            // SC-42: hard stop once accepting this charge would cross the НПД annual income cap.
            Result incomeGate = await npdIncomeService.EnsureCanAcceptPaymentAsync(item.Price, cancellationToken);
            if (incomeGate.IsFailure)
            {
                return Result.Failure<ServiceOrderCheckoutResponse>(incomeGate.Error);
            }

            ServiceOrder order = ServiceOrder.CreatePending(
                userId, command.ServiceType, command.TargetId, item.Price, item.Currency, utcNow);
            Payment payment = Payment.CreatePendingForServiceOrder(
                userId, order.Id, PaymentProvider.YooKassa, item.Price, item.Currency, utcNow);

            context.ServiceOrders.Add(order);
            context.Payments.Add(payment);
            await context.SaveChangesAsync(cancellationToken);

            var input = new CreatePaymentInput(
                Amount: payment.Amount,
                Currency: item.Currency,
                Description: item.Description,
                ReturnUrl: checkoutOptions.Value.ReturnUrl,
                IdempotenceKey: payment.Id.ToString(),
                CustomerEmail: customerEmail,
                PaymentId: payment.Id,
                UserId: userId,
                ServiceOrderId: order.Id);

            CreatedPayment created;
            try
            {
                created = await paymentProvider.CreatePaymentAsync(input, cancellationToken);
            }
            catch (PaymentProviderException ex)
            {
                // The pending order/payment stay persisted; the payment id is the idempotence key, so a
                // retry reuses it and never double-charges.
                logger.LogError(ex, "Failed to create YooKassa payment for service order {ServiceOrderId}", order.Id);
                return Result.Failure<ServiceOrderCheckoutResponse>(
                    ex.IsTransient
                        ? PaymentErrors.ProviderUnavailable(ex.Message)
                        : PaymentErrors.ProviderError(ex.Message));
            }

            payment.AssignProviderPayment(created.ProviderPaymentId, created.ConfirmationUrl);
            await context.SaveChangesAsync(cancellationToken);

            return new ServiceOrderCheckoutResponse
            {
                ServiceOrderId = order.Id,
                PaymentId = payment.Id,
                ConfirmationUrl = created.ConfirmationUrl,
            };
        }

        /// <summary>
        /// Checks that the requested target exists, that this buyer may buy this service for it, and
        /// that they are not paying twice for access they already hold.
        /// </summary>
        private async Task<Result> ValidateTargetAsync(
            CreateServiceOrderCheckoutCommand command,
            Guid userId,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            if (!ServiceTargets.RequiresTarget(command.ServiceType))
            {
                return Result.Success();
            }
            if (command.TargetId is not Guid targetId || targetId == Guid.Empty)
            {
                return Result.Failure(ServiceOrderErrors.TargetRequired);
            }

            switch (command.ServiceType)
            {
                case ServiceType.ScoringReport:
                {
                    // Buying insight into someone else's startup is the point, so membership is not
                    // required — but a banned startup is not on sale.
                    bool visible = await context.Startups
                        .AsNoTracking()
                        .AnyAsync(
                            s => s.Id == targetId
                              && !(s.IsBanned && (s.BanExpiresAt == null || s.BanExpiresAt > utcNow)),
                            cancellationToken);
                    if (!visible)
                    {
                        return Result.Failure(ServiceOrderErrors.TargetNotFound(targetId));
                    }
                    break;
                }

                case ServiceType.TermSheet:
                {
                    // Only the investor side of a deal is Pro-gated, so only the investor can buy their
                    // way past that gate.
                    Guid? investorProfileId = await context.InvestmentDeals
                        .AsNoTracking()
                        .Where(d => d.Id == targetId)
                        .Select(d => (Guid?)d.InvestorProfileId)
                        .SingleOrDefaultAsync(cancellationToken);
                    if (investorProfileId is null)
                    {
                        return Result.Failure(ServiceOrderErrors.TargetNotFound(targetId));
                    }
                    if (investorProfileId != userId)
                    {
                        return Result.Failure(ServiceOrderErrors.TargetNotAllowed);
                    }
                    break;
                }

                case ServiceType.Promotion:
                {
                    bool exists = await context.Startups
                        .AsNoTracking()
                        .AnyAsync(s => s.Id == targetId, cancellationToken);
                    if (!exists)
                    {
                        return Result.Failure(ServiceOrderErrors.TargetNotFound(targetId));
                    }

                    // Promotion changes how a startup is shown, so only the people who run it may buy it.
                    bool canPromote = await context.StartupMembers
                        .AsNoTracking()
                        .AnyAsync(
                            sm => sm.StartupId == targetId
                               && sm.ProfileId == userId
                               && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration),
                            cancellationToken);
                    if (!canPromote)
                    {
                        return Result.Failure(ServiceOrderErrors.TargetNotAllowed);
                    }
                    break;
                }
            }

            // Re-buying access that is already running would take money for nothing. Promotion is the
            // exception: another purchase genuinely adds days on top of the remaining ones.
            if (command.ServiceType != ServiceType.Promotion)
            {
                bool alreadyOwned = await context.ServiceOrders
                    .AsNoTracking()
                    .AnyAsync(
                        o => o.UserId == userId
                          && o.ServiceType == command.ServiceType
                          && o.TargetId == targetId
                          && o.Status == ServiceOrderStatus.Fulfilled
                          && (o.ExpiresAt == null || o.ExpiresAt > utcNow),
                        cancellationToken);
                if (alreadyOwned)
                {
                    return Result.Failure(ServiceOrderErrors.AlreadyOwned);
                }
            }

            return Result.Success();
        }
    }
}
