using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Subscriptions;
using DevStart.Domain.Payments;
using DevStart.Domain.ServiceOrders;
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
                userId, command.ServiceType, item.Price, item.Currency, utcNow);
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
    }
}
