using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.OAuth;
using DevStart.Application.Auth.OAuth.Callback;
using DevStart.Domain.ExternalLogins;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace DevStart.WebApi.Endpoints.Auth
{
    internal sealed class OAuthCallback : IEndpoint
    {
        internal const string DeviceTokenHeader = "X-Device-Token";

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/auth/oauth/{provider}/callback", async (
                string provider,
                string code,
                string state,
                HttpContext httpContext,
                ICommandHandler<HandleOAuthCallbackCommand, OAuthAuthResult> handler,
                CancellationToken cancellationToken) =>
            {
                if (!Enum.TryParse<ExternalLoginProvider>(provider, ignoreCase: true, out var parsed))
                {
                    return Results.NotFound();
                }

                string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
                string? ua = httpContext.Request.Headers.UserAgent.ToString();

                // A header, not a query parameter: this is a GET, and query strings end up in nginx
                // access logs and Serilog request logging. The SPA sends it only on this call.
                string? deviceToken = httpContext.Request.Headers[DeviceTokenHeader].FirstOrDefault();

                var command = new HandleOAuthCallbackCommand(parsed, code, state, ip, ua, deviceToken);

                Result<OAuthAuthResult> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Auth)
            .RequireRateLimiting("auth");
        }
    }
}
