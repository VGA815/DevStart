using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;

namespace DevStart.Application.Admin.Valuation.SaveBenchmarkIndustryMapping
{
    /// <summary>
    /// Upserts a mapping by its natural key (<paramref name="SourceKind"/>, <paramref name="ExternalKey"/>) —
    /// there is no separate create/edit, because a bucket name is the identity.
    ///
    /// <paramref name="Industry"/> <c>null</c> records "deliberately not mapped", which is what takes a
    /// bucket off the work queue without pretending it belongs to a sector.
    /// </summary>
    public sealed record SaveBenchmarkIndustryMappingCommand(
        BenchmarkMappingSourceKind SourceKind,
        string ExternalKey,
        Industry? Industry,
        string? Note) : ICommand<Guid>;
}
