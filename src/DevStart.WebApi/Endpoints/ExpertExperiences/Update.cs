using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertExperiences.Update;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.ExpertExperiences
{
    internal sealed class Update : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("id")] Guid Id,
            [property: JsonPropertyName("company")] string Company,
            [property: JsonPropertyName("position")] string Position,
            [property: JsonPropertyName("start_date")] DateOnly StartDate,
            [property: JsonPropertyName("end_date")] DateOnly? EndDate,
            [property: JsonPropertyName("description")] string? Description);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("api/expert-experiences", async (
                [FromBody] Request request,
                ICommandHandler<UpdateExpertExperienceCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateExpertExperienceCommand(
                    request.Id,
                    request.Company,
                    request.Position,
                    request.StartDate,
                    request.EndDate,
                    request.Description);

                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertExperiencesUpdate)
                .WithTags(Tags.ExpertExperiences);
        }
    }
}
