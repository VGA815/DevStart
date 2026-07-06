using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.TwoFactor.Disable;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class TwoFactorDisable : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("password")] string? Password,
            [property: JsonPropertyName("code")] string Code);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/users/me/2fa/disable", async (
                Request request,
                ICommandHandler<DisableTwoFactorCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.Handle(
                    new DisableTwoFactorCommand(request.Password, request.Code), cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .WithTags(Tags.Users)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
