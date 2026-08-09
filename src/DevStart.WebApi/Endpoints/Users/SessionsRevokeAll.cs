using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.Sessions.RevokeAllSessions;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class SessionsRevokeAll : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("include_current")] bool IncludeCurrent = false);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/users/me/sessions/revoke-all", async (
                [FromBody] Request? request,
                ICommandHandler<RevokeAllSessionsCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.Handle(
                    new RevokeAllSessionsCommand(request?.IncludeCurrent ?? false), cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .WithTags(Tags.Sessions)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
