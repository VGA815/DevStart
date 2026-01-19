
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupMembers.ChangeVisibility;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupMembers
{
    internal sealed class ChangeVisibility : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("profile_id")] Guid ProfileId,
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("is_public")] bool IsPublic);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("startups/members/visibility", async (
                [FromBody] Request request, 
                ICommandHandler<ChangeStartupMemberVisibilityCommand> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new ChangeStartupMemberVisibilityCommand(request.ProfileId, request.StartupId, request.IsPublic);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupMembers);
        }
    }
}
