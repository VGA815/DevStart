
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups.GetAllByProfileId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Startups
{
    internal sealed class GetAllByProfileId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("startups", async (
                [FromQuery] Guid profileId, 
                IQueryHandler<GetStartupsByProfileIdQuery, List<StartupResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupsByProfileIdQuery(profileId);

                Result<List<StartupResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Startups);
        }
    }
}
