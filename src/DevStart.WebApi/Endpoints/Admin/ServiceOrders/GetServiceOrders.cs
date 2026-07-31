using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.ServiceOrders.GetServiceOrders;
using DevStart.Domain.ServiceOrders;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.ServiceOrders
{
    internal sealed class GetServiceOrders : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/service-orders", async (
                IQueryHandler<GetAdminServiceOrdersQuery, List<AdminServiceOrderResponse>> handler,
                CancellationToken cancellationToken,
                [FromQuery] Guid? userId = null,
                [FromQuery] ServiceOrderStatus? status = null,
                [FromQuery] ServiceType? serviceType = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 50) =>
            {
                var query = new GetAdminServiceOrdersQuery(userId, status, serviceType, pageNumber, pageSize);
                Result<List<AdminServiceOrderResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminServiceOrdersRead)
                .WithTags(Tags.Admin);
        }
    }
}
