using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups.Create;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Startups
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("user_id")] Guid UserId,
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("public_email")] string PublicEmail,
            [property: JsonPropertyName("description")] string Description,
            [property: JsonPropertyName("url")] string Url,
            [property: JsonPropertyName("is_stopped")] bool IsStopped,
            [property: JsonPropertyName("stage")] StartupStage Stage,
            [property: JsonPropertyName("social_media_links")] List<string> SocialMediaLinks,
            [property: JsonPropertyName("location")] StartupLocation StartupLocation,
            [property: JsonPropertyName("billing_email")] string BillingEmail,
            [property: JsonPropertyName("avatar_id")] Guid? AvatarId,
            [property: JsonPropertyName("product_problem")] string? ProductProblem,
            [property: JsonPropertyName("product_solution")] string ProductSolution,
            [property: JsonPropertyName("stack")] List<string> Stack,
            [property: JsonPropertyName("product_value_proposition")] string? ProductValueProposition,
            [property: JsonPropertyName("product_differentiators")] string? ProductDifferentiators,
            [property: JsonPropertyName("short_description")] string ShortDescription,
            [property: JsonPropertyName("tam")] decimal? Tam = null,
            [property: JsonPropertyName("sam")] decimal? Sam = null,
            [property: JsonPropertyName("som")] decimal? Som = null,
            [property: JsonPropertyName("market_growth_rate")] decimal? MarketGrowthRate = null,
            [property: JsonPropertyName("has_patents")] bool HasPatents = false,
            [property: JsonPropertyName("industry")] Industry Industry = Industry.Other,
            [property: JsonPropertyName("target_round_amount")] decimal? TargetRoundAmount = null,
            [property: JsonPropertyName("has_strategic_partnerships")] bool HasStrategicPartnerships = false);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/startups/", async (
                [FromBody] Request request,
                ICommandHandler<CreateStartupCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateStartupCommand(
                    request.UserId,
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
                    request.ProductProblem,
                    request.ProductSolution,
                    request.Stack,
                    request.ProductValueProposition,
                    request.ProductDifferentiators,
                    request.Tam,
                    request.Sam,
                    request.Som,
                    request.MarketGrowthRate,
                    request.HasPatents,
                    request.Industry,
                    request.TargetRoundAmount,
                    request.HasStrategicPartnerships);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupsCreate)
                .WithTags(Tags.Startups);
        }
    }
}