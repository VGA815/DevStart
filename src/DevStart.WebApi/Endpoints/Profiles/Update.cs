
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Profiles.Update;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Profiles
{
    internal sealed class Update : IEndpoint
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
            app.MapPut("profiles", async (
                [FromBody] Request request, 
                ICommandHandler<UpdateProfileCommand> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateProfileCommand(
                    request.UserId,
                    request.Url,
                    request.Name,
                    request.AvatarUrl,
                    request.Bio,
                    request.IsPublic,
                    request.IsAvailableForHire,
                    request.SocialMediaLinks);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.Profiles);
        }
    }
}
