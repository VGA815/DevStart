using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Valuation.GetBenchmarkSuggestions;
using DevStart.Application.Admin.Valuation.RunBenchmarkCollection;
using DevStart.Application.Admin.Valuation.UploadDamodaranDataset;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Valuation
{
    /// <summary>
    /// The derivation side: preview suggestions, feed staging, run the collectors on demand.
    ///
    /// The suggestions endpoint is read-only by construction — it takes the derivation parameters as
    /// query inputs, stores none of them, and writes nothing. Accepting a suggestion is a separate act
    /// that goes through the existing add-benchmark command, so <c>created_by</c> stays the name of the
    /// person answering for the number.
    /// </summary>
    internal sealed class BenchmarkDerivationEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/valuation-benchmarks/suggestions", async (
                [FromQuery] int? minComparables,
                [FromQuery] decimal? countryDiscount,
                [FromQuery] decimal? illiquidityAndSizeDiscount,
                [FromQuery] string? datasetRegion,
                [FromQuery] DateTime? asOf,
                IQueryHandler<GetBenchmarkSuggestionsQuery, BenchmarkSuggestionsResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetBenchmarkSuggestionsQuery(
                    minComparables, countryDiscount, illiquidityAndSizeDiscount, datasetRegion, asOf);
                Result<BenchmarkSuggestionsResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminValuationBenchmarksRead)
                .WithTags(Tags.Admin);

            app.MapPost("api/admin/valuation-benchmarks/damodaran", async (
                IFormFile file,
                [FromQuery] int datasetYear,
                [FromQuery] string datasetRegion,
                ICommandHandler<UploadDamodaranDatasetCommand, UploadDamodaranDatasetResponse> handler,
                CancellationToken cancellationToken) =>
            {
                await using Stream stream = file.OpenReadStream();

                var command = new UploadDamodaranDatasetCommand(
                    stream, file.Length, file.FileName, file.ContentType, datasetYear, datasetRegion);

                Result<UploadDamodaranDatasetResponse> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .DisableAntiforgery()
                .HasPermission(Permissions.AdminValuationBenchmarksManage)
                .WithTags(Tags.Admin);

            app.MapPost("api/admin/valuation-benchmarks/collect", async (
                [FromQuery] BenchmarkCollectionKind kind,
                ICommandHandler<RunBenchmarkCollectionCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.Handle(new RunBenchmarkCollectionCommand(kind), cancellationToken);
                return result.Match(() => Results.Accepted(), CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminValuationBenchmarksManage)
                .WithTags(Tags.Admin);
        }
    }
}
