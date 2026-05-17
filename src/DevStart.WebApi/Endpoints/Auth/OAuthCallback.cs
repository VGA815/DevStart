using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;
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
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/auth/oauth/{provider}/callback", async (
                string provider,
                string code,
                string state,
                HttpContext httpContext,
                ICommandHandler<HandleOAuthCallbackCommand, TokenPair> handler,
                CancellationToken cancellationToken) =>
            {
                if (!Enum.TryParse<ExternalLoginProvider>(provider, ignoreCase: true, out var parsed))
                {
                    return Results.NotFound();
                }

                string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
                string? ua = httpContext.Request.Headers.UserAgent.ToString();

                var command = new HandleOAuthCallbackCommand(parsed, code, state, ip, ua);

                Result<TokenPair> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Auth);
        }
    }
}
