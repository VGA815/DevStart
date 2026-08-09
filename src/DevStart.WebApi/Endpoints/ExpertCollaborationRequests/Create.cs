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
        /// <param name="ExpertProfileId">
        /// Required when a startup invites an expert; omitted when an expert applies to a startup. The
        /// handler decides the direction from the caller, so supplying someone else's id as an expert
        /// is rejected rather than honoured.
        /// </param>
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("expert_profile_id")] Guid? ExpertProfileId,
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
                    request.ExpertProfileId,
                    request.CollaborationType,
                    request.Message,
                    request.ProposedHoursPerWeek,
                    request.ProposedRate);

                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertCollaborationRequestsCreate)
                // One pending request per counterparty still lets a single account fan out across every
                // startup or expert on the platform; the per-user bucket is what bounds that.
                .RequireRateLimiting("per-user")
                .WithTags(Tags.ExpertCollaborationRequests);
        }
    }
}
