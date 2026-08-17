using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Valuation.GetBenchmarkIndustryMappings
{
    internal sealed class GetBenchmarkIndustryMappingsQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetBenchmarkIndustryMappingsQuery, List<BenchmarkIndustryMappingResponse>>
    {
        public async Task<Result<List<BenchmarkIndustryMappingResponse>>> Handle(
            GetBenchmarkIndustryMappingsQuery query,
            CancellationToken cancellationToken)
        {
            List<BenchmarkIndustryMappingResponse> mappings = await context.BenchmarkIndustryMappings
                .AsNoTracking()
                .Where(m => query.SourceKind == null || m.SourceKind == query.SourceKind)
                .OrderBy(m => m.SourceKind)
                .ThenBy(m => m.ExternalKey)
                .Select(m => new BenchmarkIndustryMappingResponse
                {
                    Id = m.Id,
                    SourceKind = m.SourceKind,
                    ExternalKey = m.ExternalKey,
                    Industry = m.Industry,
                    Note = m.Note,
                })
                .ToListAsync(cancellationToken);

            return mappings;
        }
    }
}
