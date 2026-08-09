using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.TrustedDevices.GetTrustedDevices;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class TrustedDevices : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/users/me/devices", async (
                IQueryHandler<GetTrustedDevicesQuery, IReadOnlyList<TrustedDeviceResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyList<TrustedDeviceResponse>> result =
                    await handler.Handle(new GetTrustedDevicesQuery(), cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Sessions)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
