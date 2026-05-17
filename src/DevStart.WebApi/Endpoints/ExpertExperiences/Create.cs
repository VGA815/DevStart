using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertExperiences.Create;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.ExpertExperiences
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("expert_profile_id")] Guid ExpertProfileId,
            [property: JsonPropertyName("company")] string Company,
            [property: JsonPropertyName("position")] string Position,
            [property: JsonPropertyName("start_date")] DateOnly StartDate,
            [property: JsonPropertyName("end_date")] DateOnly? EndDate,
            [property: JsonPropertyName("description")] string? Description);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/expert-experiences", async (
                [FromBody] Request request,
                ICommandHandler<CreateExpertExperienceCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateExpertExperienceCommand(
                    request.ExpertProfileId,
                    request.Company,
                    request.Position,
                    request.StartDate,
                    request.EndDate,
                    request.Description);

                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertExperiencesCreate)
                .WithTags(Tags.ExpertExperiences);
        }
    }
}
