using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupRoadmapItems.GetAllByStartupId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupRoadmapItems
{
    internal sealed class GetAllByStartupId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("startups/{startupId:guid}/roadmap/items", async (
                Guid startupId, 
                IQueryHandler<GetStartupRoadmapItemsByStartupIdQuery, List<StartupRoadmapItemResponse>> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupRoadmapItemsByStartupIdQuery(startupId);

                Result<List<StartupRoadmapItemResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.StartupRoadmapItems);
        }
    }
}