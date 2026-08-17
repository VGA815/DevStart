using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Valuation.GetUnmappedBenchmarkBuckets
{
    /// <summary>
    /// The SC-58 work queue: Damodaran buckets sitting in staging that no mapping row covers. Derived,
    /// not stored — an unmapped bucket is a join miss, so this list can never disagree with the mapping
    /// table.
    /// </summary>
    public sealed record GetUnmappedBenchmarkBucketsQuery : IQuery<List<UnmappedBenchmarkBucketResponse>>;
}
