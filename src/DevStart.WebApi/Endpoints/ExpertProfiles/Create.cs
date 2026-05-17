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
            [property: JsonPropertyName("display_name")] string DisplayName,
            [property: JsonPropertyName("bio")] string? Bio,
            [property: JsonPropertyName("website")] string? Website,
            [property: JsonPropertyName("is_public")] bool IsPublic,
            [property: JsonPropertyName("linkedin_url")] string? LinkedInUrl,
            [property: JsonPropertyName("twitter_url")] string? TwitterUrl,
            [property: JsonPropertyName("github_url")] string? GitHubUrl,
            [property: JsonPropertyName("telegram_url")] string? TelegramUrl,
            [property: JsonPropertyName("specializations")] List<ExpertSpecialization> Specializations);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/expert-profiles", async (
                [FromBody] Request request,
                ICommandHandler<CreateExpertProfileCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateExpertProfileCommand(
                    request.DisplayName,
                    request.Bio,
                    request.Website,
                    request.IsPublic,
                    request.LinkedInUrl,
                    request.TwitterUrl,
                    request.GitHubUrl,
                    request.TelegramUrl,
                    request.Specializations ?? new List<ExpertSpecialization>());

                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertProfilesCreate)
                .WithTags(Tags.ExpertProfiles);
        }
    }
}
