using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Valuation.AddValuationBenchmark;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Valuation
{
    internal sealed class AddBenchmark : IEndpoint
    {
        public sealed record Request(
            BenchmarkMetricType MetricType,
            Industry Industry,
            StartupStage? Stage,
            decimal Value,
            string? Currency,
            DateTime EffectiveFrom,
            string Source);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/admin/valuation-benchmarks", async (
                [FromBody] Request request,
                ICommandHandler<AddValuationBenchmarkCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new AddValuationBenchmarkCommand(
                    request.MetricType,
                    request.Industry,
                    request.Stage,
                    request.Value,
                    request.Currency,
                    request.EffectiveFrom,
                    request.Source);
                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminValuationBenchmarksManage)
                .WithTags(Tags.Admin);
        }
    }
}
