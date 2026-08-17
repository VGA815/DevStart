using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;

namespace DevStart.Application.Admin.Valuation
{
    /// <summary>One curated comparable as returned by the admin registry endpoints.</summary>
    public sealed class BenchmarkIssuerResponse
    {
        public Guid Id { get; init; }
        public string Ticker { get; init; } = null!;
        public string? Inn { get; init; }
        public string DisplayName { get; init; } = null!;
        public Industry Industry { get; init; }
        public bool IsActive { get; init; }
        public decimal? RevenueOverride { get; init; }
        public int? RevenueOverrideFiscalYear { get; init; }
        public string? RevenueOverrideNote { get; init; }
        public string? Note { get; init; }

        /// <summary>Latest collected market capitalisation, if any — lets the admin see a dead ticker.</summary>
        public decimal? LatestMarketCap { get; init; }
        public DateTime? LatestMarketCapAsOf { get; init; }

        /// <summary>Latest revenue that would actually be used (override wins), with its fiscal year.</summary>
        public decimal? LatestRevenue { get; init; }
        public int? LatestRevenueFiscalYear { get; init; }
        public bool LatestRevenueIsManual { get; init; }
    }

    /// <summary>One external-taxonomy mapping as returned by the admin registry endpoints.</summary>
    public sealed class BenchmarkIndustryMappingResponse
    {
        public Guid Id { get; init; }
        public BenchmarkMappingSourceKind SourceKind { get; init; }
        public string ExternalKey { get; init; } = null!;

        /// <summary><c>null</c> means "deliberately not mapped", which is different from "not decided yet".</summary>
        public Industry? Industry { get; init; }
        public string? Note { get; init; }
    }

    /// <summary>
    /// A Damodaran bucket present in staging that no mapping row covers — neither mapped to a sector nor
    /// explicitly excluded. This list is the SC-58 work queue, and it is derived rather than stored: an
    /// unmapped bucket is simply a join miss, so it cannot drift out of sync with the mapping table.
    /// </summary>
    public sealed class UnmappedBenchmarkBucketResponse
    {
        public string ExternalKey { get; init; } = null!;
        public decimal Value { get; init; }
        public DateTime AsOf { get; init; }
        public string? DatasetRegion { get; init; }
    }
}
