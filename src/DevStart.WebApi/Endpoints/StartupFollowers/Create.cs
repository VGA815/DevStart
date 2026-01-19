
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupFollowers.Create;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupFollowers
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("profile_id")] Guid ProfileId);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("startups/followers", async (
                [FromBody] Request request, 
                ICommandHandler<CreateStartupFollowerCommand, (Guid startupId, Guid profileId)> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new CreateStartupFollowerCommand(request.StartupId, request.ProfileId);

                Result<(Guid startupId, Guid profileId)> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupFollowers);
        }

    }
}
