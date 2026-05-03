
using DevStart.Application.Abstractions.Authorization;
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
            [property: JsonPropertyName("avatar_url")] Guid? AvatarId,
            [property: JsonPropertyName("short_description")] string? ShortDescription,
            [property: JsonPropertyName("tam")] decimal? Tam = null,
            [property: JsonPropertyName("sam")] decimal? Sam = null,
            [property: JsonPropertyName("som")] decimal? Som = null,
            [property: JsonPropertyName("market_growth_rate")] decimal? MarketGrowthRate = null,
            [property: JsonPropertyName("has_patents")] bool HasPatents = false);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("api/startups", async (
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
                    request.AvatarId,
                    request.ShortDescription,
                    request.Tam,
                    request.Sam,
                    request.Som,
                    request.MarketGrowthRate,
                    request.HasPatents);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupsUpdate)
                .WithTags(Tags.Startups);
        }
    }
}
