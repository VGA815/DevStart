using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.OAuth.Start;
using DevStart.Domain.ExternalLogins;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Auth
{
    internal sealed class OAuthLinkStart : IEndpoint
    {
        public sealed record Request(string? RedirectUri);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/auth/oauth/{provider}/link/start", async (
                string provider,
                Request request,
                IUserContext userContext,
                ICommandHandler<StartOAuthCommand, StartOAuthResponse> handler,
                CancellationToken cancellationToken) =>
            {
                if (!Enum.TryParse<ExternalLoginProvider>(provider, ignoreCase: true, out var parsed))
                {
                    return Results.NotFound();
                }

                var command = new StartOAuthCommand(parsed, request.RedirectUri, userContext.UserId);

                Result<StartOAuthResponse> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .RequireAuthorization()
            .WithTags(Tags.Auth);
        }
    }
}
