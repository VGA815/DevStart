using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.TwoFactor.SetupLogin;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Auth
{
    /// <summary>
    /// Login-time mandatory 2FA enrollment (admins). Authenticated by the pending token issued at
    /// login, not by a bearer token — the caller has no tokens yet.
    /// </summary>
    internal sealed class TwoFactorSetup : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("pending_token")] string PendingToken);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/auth/2fa/setup", async (
                [FromBody] Request request,
                ICommandHandler<SetupTwoFactorLoginCommand, TwoFactorLoginSetupResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new SetupTwoFactorLoginCommand(request.PendingToken);

                Result<TwoFactorLoginSetupResponse> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Auth)
                .RequireRateLimiting("auth");
        }
    }
}
