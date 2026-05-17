using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.Logout;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Auth
{
    internal sealed class Logout : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("refresh_token")] string RefreshToken);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/auth/logout", async (
                Request request,
                ICommandHandler<LogoutCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new LogoutCommand(request.RefreshToken);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(Tags.Auth);
        }
    }
}
