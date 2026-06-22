using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Users.GetUsers;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Users
{
    internal sealed class GetUsers : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/users", async (
                IQueryHandler<GetUsersQuery, List<AdminUserListItemResponse>> handler,
                CancellationToken cancellationToken,
                [FromQuery] string? search = null,
                [FromQuery] UserSystemRole? role = null,
                [FromQuery] bool? isBanned = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 50) =>
            {
                var query = new GetUsersQuery(search, role, isBanned, pageNumber, pageSize);
                Result<List<AdminUserListItemResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminUsersRead)
                .WithTags(Tags.Admin);
        }
    }
}
