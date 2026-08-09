using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.Sessions.RevokeSession;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class SessionRevoke : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/users/me/sessions/{sessionId:guid}", async (
                Guid sessionId,
                ICommandHandler<RevokeSessionCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.Handle(new RevokeSessionCommand(sessionId), cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .WithTags(Tags.Sessions)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
