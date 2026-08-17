using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.Valuation;
using DevStart.Application.Admin.Valuation.DeleteBenchmarkIndustryMapping;
using DevStart.Application.Admin.Valuation.GetBenchmarkIndustryMappings;
using DevStart.Application.Admin.Valuation.GetBenchmarkIssuers;
using DevStart.Application.Admin.Valuation.GetUnmappedBenchmarkBuckets;
using DevStart.Application.Admin.Valuation.SaveBenchmarkIndustryMapping;
using DevStart.Application.Admin.Valuation.SaveBenchmarkIssuer;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Admin.Valuation
{
    /// <summary>
    /// The SC-58 registry: which external object belongs to which <see cref="Industry"/>. Reads sit
    /// behind <c>admin_valuation_benchmarks::read</c>, writes behind <c>::manage</c> — the same split
    /// the benchmark endpoints use.
    /// </summary>
    internal sealed class BenchmarkRegistryEndpoints : IEndpoint
    {
        public sealed record SaveIssuerRequest(
            Guid? Id,
            string Ticker,
            string? Inn,
            string DisplayName,
            Industry Industry,
            bool IsActive,
            decimal? RevenueOverride,
            int? RevenueOverrideFiscalYear,
            string? RevenueOverrideNote,
            string? Note);

        public sealed record SaveMappingRequest(
            BenchmarkMappingSourceKind SourceKind,
            string ExternalKey,
            Industry? Industry,
            string? Note);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/valuation-benchmarks/issuers", async (
                IQueryHandler<GetBenchmarkIssuersQuery, List<BenchmarkIssuerResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                Result<List<BenchmarkIssuerResponse>> result =
                    await handler.Handle(new GetBenchmarkIssuersQuery(), cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminValuationBenchmarksRead)
                .WithTags(Tags.Admin);

            app.MapPost("api/admin/valuation-benchmarks/issuers", async (
                [FromBody] SaveIssuerRequest request,
                ICommandHandler<SaveBenchmarkIssuerCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new SaveBenchmarkIssuerCommand(
                    request.Id,
                    request.Ticker,
                    request.Inn,
                    request.DisplayName,
                    request.Industry,
                    request.IsActive,
                    request.RevenueOverride,
                    request.RevenueOverrideFiscalYear,
                    request.RevenueOverrideNote,
                    request.Note);
                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminValuationBenchmarksManage)
                .WithTags(Tags.Admin);

            app.MapGet("api/admin/valuation-benchmarks/industry-mappings", async (
                [FromQuery] BenchmarkMappingSourceKind? sourceKind,
                IQueryHandler<GetBenchmarkIndustryMappingsQuery, List<BenchmarkIndustryMappingResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                Result<List<BenchmarkIndustryMappingResponse>> result =
                    await handler.Handle(new GetBenchmarkIndustryMappingsQuery(sourceKind), cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminValuationBenchmarksRead)
                .WithTags(Tags.Admin);

            app.MapPost("api/admin/valuation-benchmarks/industry-mappings", async (
                [FromBody] SaveMappingRequest request,
                ICommandHandler<SaveBenchmarkIndustryMappingCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new SaveBenchmarkIndustryMappingCommand(
                    request.SourceKind, request.ExternalKey, request.Industry, request.Note);
                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminValuationBenchmarksManage)
                .WithTags(Tags.Admin);

            app.MapDelete("api/admin/valuation-benchmarks/industry-mappings/{id:guid}", async (
                Guid id,
                ICommandHandler<DeleteBenchmarkIndustryMappingCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.Handle(new DeleteBenchmarkIndustryMappingCommand(id), cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminValuationBenchmarksManage)
                .WithTags(Tags.Admin);

            app.MapGet("api/admin/valuation-benchmarks/unmapped-buckets", async (
                IQueryHandler<GetUnmappedBenchmarkBucketsQuery, List<UnmappedBenchmarkBucketResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                Result<List<UnmappedBenchmarkBucketResponse>> result =
                    await handler.Handle(new GetUnmappedBenchmarkBucketsQuery(), cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminValuationBenchmarksRead)
                .WithTags(Tags.Admin);
        }
    }
}
