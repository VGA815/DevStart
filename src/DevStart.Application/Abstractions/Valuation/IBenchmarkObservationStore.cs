using DevStart.Domain.Valuation;

namespace DevStart.Application.Abstractions.Valuation
{
    /// <summary>
    /// The only write path into the benchmark staging table. Staging is a refetchable cache of external
    /// facts, so writes are upserts keyed on (source, issuer or bucket, metric, as-of): re-running a
    /// collection on the same day changes nothing, and history accumulates by itself as the quarter
    /// advances.
    ///
    /// Nothing here writes to <see cref="ValuationBenchmark"/>. That table has exactly one write path
    /// and it runs through an admin pressing Add on the existing form.
    /// </summary>
    public interface IBenchmarkObservationStore
    {
        /// <summary>
        /// Upserts a whole run's worth of issuer-level facts in one transaction. Batched rather than
        /// per-issuer because a collection run touches every registered comparable, and one transaction
        /// per issuer would turn a quarterly job into dozens of round trips for no gain: a run that
        /// half-lands is not more useful than one that does not land, and the next quarter (or a manual
        /// re-run) refetches everything anyway.
        ///
        /// Failure isolation lives in the jobs, where it belongs — an issuer whose HTTP call failed
        /// simply never reaches this list.
        /// </summary>
        Task UpsertIssuerObservationsAsync(
            IReadOnlyCollection<IssuerObservation> observations,
            CancellationToken cancellationToken);

        /// <summary>
        /// Replaces every Damodaran observation of one dataset year in a single transaction. Replace,
        /// not merge: a re-upload of the same year is a correction of that year, and leaving stale
        /// buckets behind would produce a set that is a mixture of two datasets and looks like neither.
        /// </summary>
        Task ReplaceDamodaranYearAsync(
            int datasetYear,
            string datasetRegion,
            IReadOnlyCollection<DamodaranBucketObservation> buckets,
            CancellationToken cancellationToken);
    }

    /// <summary>One collected issuer-level fact on its way into staging.</summary>
    public sealed record IssuerObservation(
        Guid IssuerId,
        BenchmarkObservationSource Source,
        BenchmarkObservationMetric Metric,
        decimal Value,
        DateTime AsOf,
        int? FiscalYear,
        string? OriginNote);

    /// <summary>One parsed row of a Damodaran industry dataset.</summary>
    public sealed record DamodaranBucketObservation(string ExternalKey, decimal EvSales);
}
