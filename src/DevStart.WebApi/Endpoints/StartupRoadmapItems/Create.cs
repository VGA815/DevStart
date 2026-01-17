
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupRoadmapItems.Create;
using DevStart.Domain.StartupRoadmapItems;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupRoadmapItems
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("startup_stage")] StartupStage StartupStage,
            [property: JsonPropertyName("title")] string Title,
            [property: JsonPropertyName("status")] RoadmapItemStatus Status,
            [property: JsonPropertyName("description")] string? Description,
            [property: JsonPropertyName("target_date")] DateTime TargetDate);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("startups/roadmap/items", async (
                [FromBody] Request request, 
                ICommandHandler<CreateStartupRoadmapItemCommand, Guid> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new CreateStartupRoadmapItemCommand(
                    request.StartupId,
                    request.StartupStage,
                    request.Title,
                    request.Description,
                    request.Status,
                    request.TargetDate);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupRoadmapItems);
        }
    }
}
