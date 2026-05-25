using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.ForgotPassword;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class ForgotPassword : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("email")] string Email);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/users/forgot-password", async (
                Request request,
                ICommandHandler<ForgotPasswordCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ForgotPasswordCommand(request.Email);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .WithTags(Tags.Users)
                .RequireRateLimiting("auth");
        }
    }
}
