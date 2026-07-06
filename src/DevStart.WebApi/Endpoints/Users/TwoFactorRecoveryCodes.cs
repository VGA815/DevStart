using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.TwoFactor.RegenerateRecoveryCodes;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class TwoFactorRecoveryCodes : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("code")] string Code);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/users/me/2fa/recovery-codes", async (
                Request request,
                ICommandHandler<RegenerateRecoveryCodesCommand, IReadOnlyList<string>> handler,
                CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyList<string>> result = await handler.Handle(
                    new RegenerateRecoveryCodesCommand(request.Code), cancellationToken);

                return result.Match(
                    codes => Results.Ok(new { recoveryCodes = codes }),
                    CustomResults.Problem);
            })
                .WithTags(Tags.Users)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
