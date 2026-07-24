using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupCompetitors.Update;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupCompetitors
{
    internal sealed class Update : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("website")] string Website,
            [property: JsonPropertyName("description")] string? Description,
            [property: JsonPropertyName("strengths_vs_us")] string? StrengthsVsUs,
            [property: JsonPropertyName("weaknesses_vs_us")] string? WeaknessesVsUs);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("api/startup-competitors/{competitorId:guid}", async (
                Guid competitorId,
                [FromBody] Request request,
                ICommandHandler<UpdateStartupCompetitorCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateStartupCompetitorCommand(
                    competitorId,
                    request.Name,
                    request.Website,
                    request.Description,
                    request.StrengthsVsUs,
                    request.WeaknessesVsUs);

                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupCompetitorsUpdate)
                .WithTags(Tags.StartupCompetitors);
        }
    }
}
