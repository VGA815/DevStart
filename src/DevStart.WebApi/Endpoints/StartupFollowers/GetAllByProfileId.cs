using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupFollowers.GetAllByProfileId;
using DevStart.Application.Startups.GetAllByProfileId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.StartupFollowers
{
    internal sealed class GetAllByProfileId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/followers/by-profile", async (
                [FromQuery] Guid profileId,
                IQueryHandler<GetStartupsByProfileFollowsQuery, List<StartupResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupsByProfileFollowsQuery(profileId);
                Result<List<StartupResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.StartupFollowers);
        }
    }
}
