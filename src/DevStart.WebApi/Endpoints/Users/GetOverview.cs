using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.GetOverview;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class GetOverview : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/users/{userId:guid}/overview", async (
                Guid userId,
                IQueryHandler<GetUserOverviewQuery, UserOverviewResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetUserOverviewQuery(userId);

                Result<UserOverviewResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.UsersRead)
                .WithTags(Tags.Users);
        }
    }
}
