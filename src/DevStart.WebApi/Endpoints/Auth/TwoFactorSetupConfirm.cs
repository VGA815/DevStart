using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.TwoFactor.ConfirmSetupLogin;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Auth
{
    internal sealed class TwoFactorSetupConfirm : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("pending_token")] string PendingToken,
            [property: JsonPropertyName("code")] string Code,
            [property: JsonPropertyName("remember_device")] bool RememberDevice = false);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/auth/2fa/setup/confirm", async (
                [FromBody] Request request,
                HttpContext httpContext,
                ICommandHandler<ConfirmTwoFactorSetupLoginCommand, TwoFactorSetupCompleteResponse> handler,
                CancellationToken cancellationToken) =>
            {
                string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
                string? ua = httpContext.Request.Headers.UserAgent.ToString();

                var command = new ConfirmTwoFactorSetupLoginCommand(
                    request.PendingToken, request.Code, ip, ua, request.RememberDevice);

                Result<TwoFactorSetupCompleteResponse> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Auth)
                .RequireRateLimiting("auth")
                .RequireCaptcha();
        }
    }
}
