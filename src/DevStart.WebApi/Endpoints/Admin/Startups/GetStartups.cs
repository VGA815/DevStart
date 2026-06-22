using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Startups.GetStartups;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Startups
{
    internal sealed class GetStartups : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/startups", async (
                IQueryHandler<GetAdminStartupsQuery, List<AdminStartupListItemResponse>> handler,
                CancellationToken cancellationToken,
                [FromQuery] string? search = null,
                [FromQuery] bool? isBanned = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 50) =>
            {
                var query = new GetAdminStartupsQuery(search, isBanned, pageNumber, pageSize);
                Result<List<AdminStartupListItemResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminStartupsRead)
                .WithTags(Tags.Admin);
        }
    }
}
