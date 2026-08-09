using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.Security.GetSecuritySettings;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class SecuritySettingsGet : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/users/me/security", async (
                IQueryHandler<GetSecuritySettingsQuery, SecuritySettingsResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Result<SecuritySettingsResponse> result =
                    await handler.Handle(new GetSecuritySettingsQuery(), cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Sessions)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
