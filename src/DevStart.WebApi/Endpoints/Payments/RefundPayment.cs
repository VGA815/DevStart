using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Payments.Refund;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Payments
{
    internal sealed class RefundPayment : IEndpoint
    {
        public sealed record Request(decimal? Amount);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/payments/{paymentId:guid}/refund", async (
                Guid paymentId,
                Request? request,
                ICommandHandler<RefundPaymentCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RefundPaymentCommand(paymentId, request?.Amount);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.PaymentsRefund)
                .WithTags(Tags.Payments);
        }
    }
}
