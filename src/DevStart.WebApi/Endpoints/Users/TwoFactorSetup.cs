using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.TwoFactor;
using DevStart.Application.Users.TwoFactor.Setup;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class TwoFactorSetup : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/users/me/2fa/setup", async (
                ICommandHandler<SetupTwoFactorCommand, TwoFactorSetupData> handler,
                CancellationToken cancellationToken) =>
            {
                Result<TwoFactorSetupData> result = await handler.Handle(new SetupTwoFactorCommand(), cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Users)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
