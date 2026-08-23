using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertProfiles.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Security.Claims;

namespace DevStart.WebApi.Endpoints.ExpertProfiles
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Anonymous, like the catalog that links here: requiring a permission made every logged-out
            // click on a listed expert look like "no such expert". The handler still hides a non-public
            // profile from everyone but its owner, which is the same line the catalog draws.
            app.MapGet("api/expert-profiles/{userId:guid}", async (
                Guid userId,
                ClaimsPrincipal user,
                IQueryHandler<GetExpertProfileByIdQuery, ExpertProfileResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Guid? viewerId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id)
                    ? id
                    : null;

                var query = new GetExpertProfileByIdQuery(userId, viewerId);
                Result<ExpertProfileResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.ExpertProfiles);
        }
    }
}
