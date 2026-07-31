using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ServiceOrders.GetMine;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ServiceOrders
{
    internal sealed class GetMine : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/service-orders", async (
                IQueryHandler<GetMyServiceOrdersQuery, List<ServiceOrderResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                Result<List<ServiceOrderResponse>> result =
                    await handler.Handle(new GetMyServiceOrdersQuery(), cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ServiceOrdersRead)
                .WithTags(Tags.ServiceOrders);
        }
    }
}
