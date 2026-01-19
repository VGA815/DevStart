
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupMetrics.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupMetrics
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("startups/metrics/{metricId:guid}", async (
                Guid metricId, 
                IQueryHandler<GetStartupMetricByIdQuery, StartupMetricResponse> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupMetricByIdQuery(metricId);

                Result<StartupMetricResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.StartupMetrics);
        }
    }
}
