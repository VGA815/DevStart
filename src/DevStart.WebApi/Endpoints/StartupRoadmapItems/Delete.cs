
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupRoadmapItems.Delete;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.StartupRoadmapItems
{
    internal sealed class Delete : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("startups/roadmap/items", async (
                [FromQuery] Guid itemId, 
                ICommandHandler<DeleteStartupRoadmapItemCommand> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteStartupRoadmapItemCommand(itemId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupRoadmapItems);
        }
    }
}
