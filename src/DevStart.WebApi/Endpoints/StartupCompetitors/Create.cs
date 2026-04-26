using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupCompetitors.Create;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupCompetitors
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("website")] string? Website,
            [property: JsonPropertyName("description")] string? Description,
            [property: JsonPropertyName("strengths_vs_us")] string? StrengthsVsUs,
            [property: JsonPropertyName("weaknesses_vs_us")] string? WeaknessesVsUs);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/startup-competitors", async (
                [FromBody] Request request,
                ICommandHandler<CreateStartupCompetitorCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateStartupCompetitorCommand(
                    request.StartupId,
                    request.Name,
                    request.Website,
                    request.Description,
                    request.StrengthsVsUs,
                    request.WeaknessesVsUs);

                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupCompetitorsCreate)
                .WithTags(Tags.StartupCompetitors);
        }
    }
}
