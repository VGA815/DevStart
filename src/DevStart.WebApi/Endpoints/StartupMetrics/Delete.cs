
using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupMetrics.Delete;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.StartupMetrics
{
    internal sealed class Delete : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/startups/metrics", async (
                [FromQuery] Guid metricId, 
                ICommandHandler<DeleteStartupMetricCommand> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteStartupMetricCommand(metricId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupMetricsDelete)
                .WithTags(Tags.StartupMetrics);
        }
    }
}
