using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Valuation;

namespace DevStart.Application.Admin.Valuation.GetBenchmarkIndustryMappings
{
    /// <summary>Lists the external-taxonomy mappings, optionally narrowed to one source.</summary>
    public sealed record GetBenchmarkIndustryMappingsQuery(BenchmarkMappingSourceKind? SourceKind)
        : IQuery<List<BenchmarkIndustryMappingResponse>>;
}
