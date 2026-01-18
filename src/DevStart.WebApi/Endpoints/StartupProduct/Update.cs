
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupProducts.Update;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupProduct
{
    internal sealed class Update : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("problem")] string Problem,
            [property: JsonPropertyName("solution")] string Solution,
            [property: JsonPropertyName("stack")] List<string> Stack,
            [property: JsonPropertyName("value_proposition")] string ValueProposition,
            [property: JsonPropertyName("differentiators")] string Differentiators);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("startups/products", async (
                [FromBody] Request request,
                ICommandHandler<UpdateStartupProductCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateStartupProductCommand(
                    request.StartupId,
                    request.Problem,
                    request.Solution,
                    request.Stack,
                    request.ValueProposition,
                    request.Differentiators);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupProducts);
        }
    }
}
