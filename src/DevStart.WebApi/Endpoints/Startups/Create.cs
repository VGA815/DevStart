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
            [property: JsonPropertyName("avatar_url")] string AvatarUrl,
            [property: JsonPropertyName("product_name")] string ProductName,
            [property: JsonPropertyName("product_problem_solution")] string ProductProblemSolution,
            [property: JsonPropertyName("stack")] List<string> Stack,
            [property: JsonPropertyName("product_value_proposition")] string ProductValueProposition,
            [property: JsonPropertyName("product_differentiators")] string ProductDifferentiators);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("startups/", async (
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
                    request.AvatarUrl,
                    request.ProductName,
                    request.ProductProblemSolution,
                    request.Stack,
                    request.ProductValueProposition,
                    request.ProductDifferentiators);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.Startups);
        }
    }
}