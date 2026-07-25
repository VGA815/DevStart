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
        public sealed record Request(ServiceType ServiceType);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/service-orders/checkout", async (
                [FromBody] Request request,
                ICommandHandler<CreateServiceOrderCheckoutCommand, ServiceOrderCheckoutResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateServiceOrderCheckoutCommand(request.ServiceType);
                Result<ServiceOrderCheckoutResponse> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ServiceOrdersCheckout)
                .WithTags(Tags.ServiceOrders);
        }
    }
}
