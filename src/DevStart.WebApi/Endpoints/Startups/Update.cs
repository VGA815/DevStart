
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups.Update;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Startups
{
    internal sealed class Update : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("public_email")] string PublicEmail,
            [property: JsonPropertyName("description")] string Description,
            [property: JsonPropertyName("url")] string Url,
            [property: JsonPropertyName("is_stopped")] bool IsStopped,
            [property: JsonPropertyName("stage")] StartupStage Stage,
            [property: JsonPropertyName("social_media_links")] List<string> SocialMediaLinks,
            [property: JsonPropertyName("location")] StartupLocation StartupLocation,
            [property: JsonPropertyName("billing_email")] string BillingEmail,
            [property: JsonPropertyName("avatar_url")] string AvatarUrl);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("startups", async (
                [FromBody] Request request, 
                ICommandHandler<UpdateStartupCommand> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateStartupCommand(
                    request.StartupId,
                    request.Name,
                    request.PublicEmail,
                    request.Description,
                    request.Url,
                    request.IsStopped,
                    request.Stage,
                    request.SocialMediaLinks,
                    request.StartupLocation,
                    request.BillingEmail,
                    request.AvatarUrl);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.Startups);
        }
    }
}
