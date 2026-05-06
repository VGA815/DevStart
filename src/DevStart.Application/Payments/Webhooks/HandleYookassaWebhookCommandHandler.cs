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

namespace DevStart.Application.Payments.Webhooks
{
    internal sealed class HandleYookassaWebhookCommandHandler(
        IApplicationDbContext context,
        IPaymentProvider paymentProvider,
        IDateTimeProvider dateTimeProvider,
        IOptions<PlansOptions> plansOptions,
        ILogger<HandleYookassaWebhookCommandHandler> logger)
        : ICommandHandler<HandleYookassaWebhookCommand>
    {
        public async Task<Result> Handle(HandleYookassaWebhookCommand command, CancellationToken cancellationToken)
        {
            PaymentWebhookEvent? @event = paymentProvider.ParseWebhook(command.Body);
            if (@event is null)
            {
                return Result.Failure(PaymentErrors.WebhookPayloadInvalid);
            }
            if (!@event.ShouldProcess)
            {
                return Result.Success();
            }

            Payment? payment = await context.Payments
                .SingleOrDefaultAsync(
                    p => p.Provider == PaymentProvider.YooKassa
                      && p.ProviderPaymentId == @event.ProviderPaymentId,
                    cancellationToken);
            if (payment is null)
            {
                logger.LogWarning(
                    "YooKassa webhook for unknown payment id {ProviderPaymentId}",
                    @event.ProviderPaymentId);
                return Result.Failure(PaymentErrors.NotFoundByProviderId(@event.ProviderPaymentId));
            }

            // Idempotency: if event matches current state, no-op.
            if (payment.Status == @event.NewStatus)
            {
                return Result.Success();
            }

            DateTime utcNow = dateTimeProvider.UtcNow;

            switch (@event.NewStatus)
            {
                case PaymentStatus.Succeeded:
                    {
                        Result paid = payment.MarkSucceeded(@event.EventTime);
                        if (paid.IsFailure)
                        {
                            return paid;
                        }

                        Subscription? subscription = await context.Subscriptions
                            .SingleOrDefaultAsync(s => s.Id == payment.SubscriptionId, cancellationToken);
                        if (subscription is null)
                        {
                            return Result.Failure(SubscriptionErrors.NotFound(payment.SubscriptionId));
                        }

                        Result activated = subscription.Activate(
                            utcNow,
                            plansOptions.Value.Pro.DurationDays);
                        if (activated.IsFailure)
                        {
                            return activated;
                        }
                        break;
                    }
                case PaymentStatus.Cancelled:
                    {
                        payment.MarkCancelled(utcNow);
                        Subscription? subscription = await context.Subscriptions
                            .SingleOrDefaultAsync(s => s.Id == payment.SubscriptionId, cancellationToken);
                        subscription?.MarkCancelled(utcNow);
                        break;
                    }
                case PaymentStatus.Failed:
                    {
                        payment.MarkFailed(utcNow);
                        Subscription? subscription = await context.Subscriptions
                            .SingleOrDefaultAsync(s => s.Id == payment.SubscriptionId, cancellationToken);
                        subscription?.MarkCancelled(utcNow);
                        break;
                    }
                case PaymentStatus.Pending:
                default:
                    return Result.Success();
            }

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
