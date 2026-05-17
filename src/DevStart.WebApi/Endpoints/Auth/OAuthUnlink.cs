using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.OAuth.Unlink;
using DevStart.Domain.ExternalLogins;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Auth
{
    internal sealed class OAuthUnlink : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/auth/oauth/{provider}/unlink", async (
                string provider,
                IUserContext userContext,
                ICommandHandler<UnlinkExternalLoginCommand> handler,
                CancellationToken cancellationToken) =>
            {
                if (!Enum.TryParse<ExternalLoginProvider>(provider, ignoreCase: true, out var parsed))
                {
                    return Results.NotFound();
                }

                var command = new UnlinkExternalLoginCommand(userContext.UserId, parsed);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .RequireAuthorization()
            .WithTags(Tags.Auth);
        }
    }
}
