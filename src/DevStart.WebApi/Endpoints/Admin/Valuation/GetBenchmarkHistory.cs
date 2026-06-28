using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Valuation;
using DevStart.Application.Admin.Valuation.GetValuationBenchmarkHistory;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Valuation
{
    internal sealed class GetBenchmarkHistory : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/valuation-benchmarks/history", async (
                IQueryHandler<GetValuationBenchmarkHistoryQuery, List<ValuationBenchmarkResponse>> handler,
                CancellationToken cancellationToken,
                [FromQuery] BenchmarkMetricType metricType,
                [FromQuery] Industry industry,
                [FromQuery] StartupStage? stage = null) =>
            {
                var query = new GetValuationBenchmarkHistoryQuery(metricType, industry, stage);
                Result<List<ValuationBenchmarkResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminValuationBenchmarksRead)
                .WithTags(Tags.Admin);
        }
    }
}
