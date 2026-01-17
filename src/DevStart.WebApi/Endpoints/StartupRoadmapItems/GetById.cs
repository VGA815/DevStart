
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupRoadmapItems.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.StartupRoadmapItems
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("startups/roadmap/items", async (
                [FromQuery] Guid itemId, 
                IQueryHandler<GetStartupRoadmapItemByIdQuery, StartupRoadmapItemResponse> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupRoadmapItemByIdQuery(itemId);

                Result<StartupRoadmapItemResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.StartupRoadmapItems);
        }
    }
}
