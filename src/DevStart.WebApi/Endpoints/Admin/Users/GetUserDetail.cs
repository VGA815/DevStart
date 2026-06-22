using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Users.GetUserDetail;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Admin.Users
{
    internal sealed class GetUserDetail : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/users/{id:guid}", async (
                Guid id,
                IQueryHandler<GetUserDetailQuery, AdminUserDetailResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetUserDetailQuery(id);
                Result<AdminUserDetailResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminUsersRead)
                .WithTags(Tags.Admin);
        }
    }
}
