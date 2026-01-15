
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.Login;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.Users
{
    internal sealed class Login : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("email")] string Email,
            [property: JsonPropertyName("password")] string Password);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("users/login", async (
                Request request,
                ICommandHandler<LoginUserCommand, string> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new LoginUserCommand(request.Email, request.Password);

                Result<string> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.Users);
        }
    }
}
