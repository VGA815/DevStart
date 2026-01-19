
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupMetrics.Update;
using DevStart.Domain.StartupMetrics;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DevStart.WebApi.Endpoints.StartupMetrics
{
    internal sealed class Update : IEndpoint
    {
        public sealed record Request(
            [property: JsonPropertyName("id")] Guid Id,
            [property: JsonPropertyName("startup_id")] Guid StartupId,
            [property: JsonPropertyName("metric_type")] MetricType MetricType,
            [property: JsonPropertyName("value")] decimal Value);
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("startups/metrics", async (
                [FromBody] Request request, 
                ICommandHandler<UpdateStartupMetricCommand> handler, 
                CancellationToken cancellationToken)  =>
            {
                var command = new UpdateStartupMetricCommand(request.Id, request.StartupId, request.MetricType, request.Value);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.StartupMetrics);
        }
    }
}
