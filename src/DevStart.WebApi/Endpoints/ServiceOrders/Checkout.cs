using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ServiceOrders.Checkout;
using DevStart.Domain.ServiceOrders;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.ServiceOrders
{
    internal sealed class Checkout : IEndpoint
    {
        /// <param name="TargetId">
        /// The startup (scoring report, promotion) or deal (term sheet) the service is bought for.
        /// </param>
        public sealed record Request(ServiceType ServiceType, Guid? TargetId);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/service-orders/checkout", async (
                [FromBody] Request request,
                ICommandHandler<CreateServiceOrderCheckoutCommand, ServiceOrderCheckoutResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateServiceOrderCheckoutCommand(request.ServiceType, request.TargetId);
                Result<ServiceOrderCheckoutResponse> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ServiceOrdersCheckout)
                .WithTags(Tags.ServiceOrders);
        }
    }
}
