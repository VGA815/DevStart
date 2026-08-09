using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.Security.UpdateSecuritySettings;
using DevStart.Domain.Security;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class SecuritySettingsUpdate : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("strictness")] int Strictness,
            [property: JsonPropertyName("trust_duration_days")] int TrustDurationDays,
            [property: JsonPropertyName("notify_on_new_device_login")] bool NotifyOnNewDeviceLogin);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("api/users/me/security", async (
                [FromBody] Request request,
                ICommandHandler<UpdateSecuritySettingsCommand> handler,
                CancellationToken cancellationToken) =>
            {
                // Cast unvalidated: the validator's IsInEnum rejects an out-of-range value with a 400
                // rather than letting it through as an undefined enum member.
                var command = new UpdateSecuritySettingsCommand(
                    (TwoFactorStrictness)request.Strictness,
                    request.TrustDurationDays,
                    request.NotifyOnNewDeviceLogin);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .WithTags(Tags.Sessions)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
