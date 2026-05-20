using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.Create;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.ExpertCollaborationRequests
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("collaboration_type")] CollaborationType CollaborationType,
            [property: JsonPropertyName("message")] string? Message,
            [property: JsonPropertyName("proposed_hours_per_week")] int? ProposedHoursPerWeek,
            [property: JsonPropertyName("proposed_rate")] decimal? ProposedRate);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/expert-collaboration-requests", async (
                [FromBody] Request request,
                ICommandHandler<CreateExpertCollaborationRequestCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateExpertCollaborationRequestCommand(
                    request.StartupId,
                    request.CollaborationType,
                    request.Message,
                    request.ProposedHoursPerWeek,
                    request.ProposedRate);

                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertCollaborationRequestsCreate)
                .WithTags(Tags.ExpertCollaborationRequests);
        }
    }
}
