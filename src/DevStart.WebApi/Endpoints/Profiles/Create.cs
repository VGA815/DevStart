
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Profiles.Create;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Profiles
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("user_id")] Guid UserId,
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("bio")] string? Bio,
            [property: JsonPropertyName("is_available_for_hire")] bool IsAvailableForHire,
            [property: JsonPropertyName("avatar_url")] string? AvatarUrl,
            [property: JsonPropertyName("url")] string? Url,
            [property: JsonPropertyName("is_public")] bool IsPublic,
            [property: JsonPropertyName("social_media_links")] List<string> SocialMediaLinks);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("profiles", async (
                [FromBody] Request request, 
                ICommandHandler<CreateProfileCommand, Guid> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new CreateProfileCommand(
                    request.UserId,
                    request.Name,
                    request.Bio,
                    request.AvatarUrl,
                    request.IsAvailableForHire,
                    request.IsPublic,
                    request.SocialMediaLinks,
                    request.Url);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.Profiles);
        }
    }
}
