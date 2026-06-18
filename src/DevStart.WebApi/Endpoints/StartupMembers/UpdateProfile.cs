using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupMembers.UpdateProfile;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupMembers
{
    internal sealed class UpdateProfile : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("position")] StartupPosition? Position,
            [property: JsonPropertyName("years_of_experience")] int? YearsOfExperience,
            [property: JsonPropertyName("has_prior_exit")] bool? HasPriorExit,
            [property: JsonPropertyName("previous_startups_count")] int? PreviousStartupsCount);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("api/startup-members/profile", async (
                [FromBody] Request request,
                ICommandHandler<UpdateStartupMemberProfileCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateStartupMemberProfileCommand(
                    request.StartupId,
                    request.Position,
                    request.YearsOfExperience,
                    request.HasPriorExit,
                    request.PreviousStartupsCount);

                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupMembersUpdateProfile)
                .WithTags(Tags.StartupMembers);
        }
    }
}
