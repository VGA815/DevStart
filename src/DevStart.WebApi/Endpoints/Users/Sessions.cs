using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.Sessions.GetSessions;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class Sessions : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/users/me/sessions", async (
                IQueryHandler<GetSessionsQuery, IReadOnlyList<SessionResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyList<SessionResponse>> result =
                    await handler.Handle(new GetSessionsQuery(), cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Sessions)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
