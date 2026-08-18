using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupPatents.Create;
using DevStart.Domain.StartupPatents;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupPatents
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("kind")] IntellectualPropertyKind Kind,
            [property: JsonPropertyName("number")] string Number);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/startup-patents", async (
                [FromBody] Request request,
                ICommandHandler<CreateStartupPatentCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateStartupPatentCommand(request.StartupId, request.Kind, request.Number);

                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupPatentsCreate)
                .WithTags(Tags.StartupPatents);
        }
    }
}
