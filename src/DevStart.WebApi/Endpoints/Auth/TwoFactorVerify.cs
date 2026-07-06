using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.OAuth;
using DevStart.Application.Auth.TwoFactor.VerifyLogin;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Auth
{
    internal sealed class TwoFactorVerify : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("pending_token")] string PendingToken,
            [property: JsonPropertyName("code")] string Code);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/auth/2fa/verify", async (
                [FromBody] Request request,
                HttpContext httpContext,
                ICommandHandler<VerifyTwoFactorLoginCommand, OAuthAuthResult> handler,
                CancellationToken cancellationToken) =>
            {
                string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
                string? ua = httpContext.Request.Headers.UserAgent.ToString();

                var command = new VerifyTwoFactorLoginCommand(request.PendingToken, request.Code, ip, ua);

                Result<OAuthAuthResult> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Auth)
                .RequireRateLimiting("auth");
        }
    }
}
