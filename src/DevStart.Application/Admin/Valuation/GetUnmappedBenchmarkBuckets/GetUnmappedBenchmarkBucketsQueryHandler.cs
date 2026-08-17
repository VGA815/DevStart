using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Valuation.GetUnmappedBenchmarkBuckets
{
    internal sealed class GetUnmappedBenchmarkBucketsQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetUnmappedBenchmarkBucketsQuery, List<UnmappedBenchmarkBucketResponse>>
    {
        public async Task<Result<List<UnmappedBenchmarkBucketResponse>>> Handle(
            GetUnmappedBenchmarkBucketsQuery query,
            CancellationToken cancellationToken)
        {
            HashSet<string> mapped = (await context.BenchmarkIndustryMappings
                    .AsNoTracking()
                    .Where(m => m.SourceKind == BenchmarkMappingSourceKind.Damodaran)
                    .Select(m => m.ExternalKey)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<BenchmarkObservation> buckets = await context.BenchmarkObservations
                .AsNoTracking()
                .Where(o => o.Source == BenchmarkObservationSource.Damodaran && o.ExternalKey != null)
                .ToListAsync(cancellationToken);

            // Latest dataset year wins per bucket: an old year's leftovers are not new work.
            return buckets
                .Where(o => !mapped.Contains(o.ExternalKey!))
                .GroupBy(o => o.ExternalKey!, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(o => o.AsOf).First())
                .OrderBy(o => o.ExternalKey, StringComparer.OrdinalIgnoreCase)
                .Select(o => new UnmappedBenchmarkBucketResponse
                {
                    ExternalKey = o.ExternalKey!,
                    Value = o.Value,
                    AsOf = o.AsOf,
                    DatasetRegion = o.DatasetRegion,
                })
                .ToList();
        }
    }
}
