using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.ChangePassword;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class ChangePassword : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("current_password")] string CurrentPassword,
            [property: JsonPropertyName("new_password")] string NewPassword);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/users/change-password", async (
                Request request,
                ICommandHandler<ChangePasswordCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .WithTags(Tags.Users)
                .RequireAuthorization()
                .RequireRateLimiting("per-user");
        }
    }
}
