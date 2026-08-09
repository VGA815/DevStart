using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.TrustedDevices.RevokeAllTrustedDevices;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class TrustedDevicesRevokeAll : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/users/me/devices/revoke-all", async (
                ICommandHandler<RevokeAllTrustedDevicesCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.Handle(new RevokeAllTrustedDevicesCommand(), cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .WithTags(Tags.Sessions)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
