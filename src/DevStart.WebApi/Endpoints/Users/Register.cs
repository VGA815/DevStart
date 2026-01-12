
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.Register;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class Register : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("email")] string Email,
            [property: JsonPropertyName("username")] string Username,
            [property: JsonPropertyName("password")] string Password,
            [property: JsonPropertyName("bio")] string? Bio,
            [property: JsonPropertyName("name")] string? Name,
            [property: JsonPropertyName("url")] string? Url,
            [property: JsonPropertyName("social_media_links")] List<string> SocialMediaLinks,
            [property: JsonPropertyName("is_public")] bool IsPublic);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("users/register", async (
                Request request,
                ICommandHandler<RegisterUserCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RegisterUserCommand(
                    request.Email,
                    request.Password,
                    request.Username,
                    request.Bio,
                    request.Name,
                    request.Url,
                    request.SocialMediaLinks,
                    request.IsPublic);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Users);
        }
    }
}
