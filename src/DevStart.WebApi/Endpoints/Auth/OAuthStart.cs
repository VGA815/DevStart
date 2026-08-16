using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.OAuth.Start;
using DevStart.Domain.ExternalLogins;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Auth
{
    internal sealed class OAuthStart : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/auth/oauth/{provider}/start", async (
                string provider,
                string? redirectUri,
                ICommandHandler<StartOAuthCommand, StartOAuthResponse> handler,
                CancellationToken cancellationToken) =>
            {
                if (!Enum.TryParse<ExternalLoginProvider>(provider, ignoreCase: true, out var parsed))
                {
                    return Results.NotFound();
                }

                var command = new StartOAuthCommand(parsed, redirectUri, LinkUserId: null);

                Result<StartOAuthResponse> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Auth)
            .RequireRateLimiting("auth")
            .RequireCaptcha();
        }
    }
}
