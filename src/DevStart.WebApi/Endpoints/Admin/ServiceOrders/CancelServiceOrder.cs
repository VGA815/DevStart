using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.ServiceOrders.CancelServiceOrder;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.ServiceOrders
{
    internal sealed class CancelServiceOrder : IEndpoint
    {
        public sealed record Request(string Reason);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/admin/service-orders/{serviceOrderId:guid}/cancel", async (
                Guid serviceOrderId,
                [FromBody] Request request,
                ICommandHandler<CancelServiceOrderCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CancelServiceOrderCommand(serviceOrderId, request.Reason);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminServiceOrdersManage)
                .WithTags(Tags.Admin);
        }
    }
}
