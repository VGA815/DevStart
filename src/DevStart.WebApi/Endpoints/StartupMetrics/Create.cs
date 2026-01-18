
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupMetrics.Create;
using DevStart.Domain.StartupMetrics;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupMetrics
{
    internal sealed class Create : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("metric_type")] MetricType MetricType,
            [property: JsonPropertyName("value")] decimal Value);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("startups/metrics", async (
                [FromBody] Request request, 
                ICommandHandler<CreateStartupMetricCommand, Guid> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new CreateStartupMetricCommand(request.StartupId, request.MetricType, request.Value);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupMetrics);
        }
    }
}
