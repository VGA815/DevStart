using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.ResetPassword;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class ResetPassword : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("token")] Guid Token,
            [property: JsonPropertyName("new_password")] string NewPassword);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/users/reset-password", async (
                Request request,
                ICommandHandler<ResetPasswordCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ResetPasswordCommand(request.Token, request.NewPassword);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .WithTags(Tags.Users)
                .RequireRateLimiting("auth")
                .RequireCaptcha();
        }
    }
}
