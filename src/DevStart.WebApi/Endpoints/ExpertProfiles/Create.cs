using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertProfiles.Create;
using DevStart.Domain.Experts;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.ExpertProfiles
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("specializations")] List<ExpertSpecialization> Specializations);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/expert-profiles", async (
                [FromBody] Request request,
                ICommandHandler<CreateExpertProfileCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateExpertProfileCommand(
                    request.Specializations ?? new List<ExpertSpecialization>());

                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertProfilesCreate)
                .WithTags(Tags.ExpertProfiles);
        }
    }
}
