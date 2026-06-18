using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertProfiles.Update;
using DevStart.Domain.Experts;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.ExpertProfiles
{
    internal sealed class Update : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("specializations")] List<ExpertSpecialization> Specializations);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("api/expert-profiles", async (
                [FromBody] Request request,
                ICommandHandler<UpdateExpertProfileCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateExpertProfileCommand(
                    request.Specializations ?? new List<ExpertSpecialization>());

                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertProfilesUpdate)
                .WithTags(Tags.ExpertProfiles);
        }
    }
}
