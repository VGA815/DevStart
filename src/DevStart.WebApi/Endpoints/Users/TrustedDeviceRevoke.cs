using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.TrustedDevices.RevokeTrustedDevice;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class TrustedDeviceRevoke : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/users/me/devices/{deviceId:guid}", async (
                Guid deviceId,
                ICommandHandler<RevokeTrustedDeviceCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.Handle(new RevokeTrustedDeviceCommand(deviceId), cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .WithTags(Tags.Sessions)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
