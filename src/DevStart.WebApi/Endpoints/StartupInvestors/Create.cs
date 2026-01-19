
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupInvestors.Create;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupInvestors
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("profile_id")] Guid ProfileId,
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("is_public")] bool IsPublic);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("startups/investors", async (
                [FromBody] Request request, 
                ICommandHandler<CreateStartupInvestorCommand, (Guid startupId, Guid profileId)> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new CreateStartupInvestorCommand(request.StartupId, request.ProfileId, request.IsPublic);

                Result<(Guid startupId, Guid profileId)> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupInvestors);
        }
    }
}
