using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Valuation;
using DevStart.Application.Admin.Valuation.GetValuationBenchmarks;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Valuation
{
    internal sealed class GetBenchmarks : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/valuation-benchmarks", async (
                IQueryHandler<GetValuationBenchmarksQuery, List<ValuationBenchmarkResponse>> handler,
                CancellationToken cancellationToken,
                [FromQuery] DateTime? asOf = null) =>
            {
                var query = new GetValuationBenchmarksQuery(asOf);
                Result<List<ValuationBenchmarkResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminValuationBenchmarksRead)
                .WithTags(Tags.Admin);
        }
    }
}
