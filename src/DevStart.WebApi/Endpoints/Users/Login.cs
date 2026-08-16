using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.OAuth;
using DevStart.Application.Users.Login;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class Login : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("email")] string Email,
            [property: JsonPropertyName("password")] string Password,
            [property: JsonPropertyName("device_token")] string? DeviceToken = null);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/users/login", async (
                Request request,
                HttpContext httpContext,
                ICommandHandler<LoginUserCommand, OAuthAuthResult> handler,
                CancellationToken cancellationToken) =>
            {
                string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
                string? ua = httpContext.Request.Headers.UserAgent.ToString();

                var command = new LoginUserCommand(request.Email, request.Password, ip, ua, request.DeviceToken);

                Result<OAuthAuthResult> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Users)
                .RequireRateLimiting("auth")
                .RequireCaptcha();
        }
    }
}
