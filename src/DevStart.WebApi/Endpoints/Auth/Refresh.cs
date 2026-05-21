using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.RefreshToken;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Auth
{
    internal sealed class Refresh : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("refresh_token")] string RefreshToken);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/auth/refresh", async (
                Request request,
                HttpContext httpContext,
                ICommandHandler<RefreshTokenCommand, TokenPair> handler,
                CancellationToken cancellationToken) =>
            {
                string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
                string? ua = httpContext.Request.Headers.UserAgent.ToString();

                var command = new RefreshTokenCommand(request.RefreshToken, ip, ua);
                Result<TokenPair> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Auth)
            .RequireRateLimiting("auth");
        }
    }
}
