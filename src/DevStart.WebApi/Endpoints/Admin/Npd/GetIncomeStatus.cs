using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Npd.GetIncomeStatus;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Npd
{
    internal sealed class GetIncomeStatus : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/npd/income-status", async (
                IQueryHandler<GetNpdIncomeStatusQuery, NpdIncomeStatusResponse> handler,
                CancellationToken cancellationToken,
                [FromQuery] int? year = null) =>
            {
                var query = new GetNpdIncomeStatusQuery(year);
                Result<NpdIncomeStatusResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminNpdRead)
                .WithTags(Tags.Admin);
        }
    }
}
