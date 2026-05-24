using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Payments.Sync;
using DevStart.Domain.Payments;
using DevStart.SharedKernel;
using Microsoft.Extensions.Logging;

namespace DevStart.Application.Payments.Webhooks
{
    /// <summary>
    /// Thin webhook trigger. The body is only used to identify the affected payment and is NOT
    /// trusted for state: the authoritative status is re-read from the provider inside
    /// <see cref="SyncPaymentStatusCommand"/>. Origin is verified (IP allowlist) at the endpoint.
    /// </summary>
    internal sealed class HandleYookassaWebhookCommandHandler(
        IPaymentProvider paymentProvider,
        ICommandHandler<SyncPaymentStatusCommand> syncHandler,
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

            if (@event.Kind == WebhookEventKind.Unsupported)
            {
                logger.LogDebug("Ignoring unsupported YooKassa webhook event.");
                return Result.Success();
            }

            return await syncHandler.Handle(
                new SyncPaymentStatusCommand(@event.ProviderPaymentId),
                cancellationToken);
        }
    }
}
