
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupMetrics.GetAllByStartupId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupMetrics
{
    internal sealed class GetAllByStartupId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("startups/{startupId:guid}/metrics", async (
                Guid startupId, 
                IQueryHandler<GetStartupMetricsByStartupIdQuery, List<StartupMetricResponse>> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupMetricsByStartupIdQuery(startupId);

                Result<List<StartupMetricResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.StartupMetrics);
        }
    }
}
