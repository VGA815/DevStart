using DevStart.SharedKernel;

namespace DevStart.Domain.Valuation
{
    /// <summary>Where an observation was collected from.</summary>
    public enum BenchmarkObservationSource
    {
        /// <summary>MOEX ISS — market capitalisation of a listed issuer.</summary>
        Moex = 0,

        /// <summary>ГИР БО — statutory (РСБУ) revenue of a Russian legal entity, by INN.</summary>
        GirBo = 1,

        /// <summary>An uploaded Damodaran industry dataset.</summary>
        Damodaran = 2,
    }

    /// <summary>What an observation measures.</summary>
    public enum BenchmarkObservationMetric
    {
        /// <summary>Market capitalisation, RUB.</summary>
        MarketCap = 0,

        /// <summary>Annual revenue, RUB, for the fiscal year on the observation.</summary>
        Revenue = 1,

        /// <summary>EV/Sales multiple of a Damodaran industry bucket (dimensionless).</summary>
        EvSales = 2,
    }

    /// <summary>
    /// A cached external fact on the way to becoming a benchmark. Staging, not a benchmark: this table
    /// is refetchable and therefore *not* append-only. Rows are keyed by
    /// (<see cref="Source"/>, issuer or bucket, <see cref="Metric"/>, <see cref="AsOf"/>) and upserted,
    /// so re-running a collection on the same day changes nothing while history still accumulates on
    /// its own as <see cref="AsOf"/> advances each quarter.
    ///
    /// Nothing here is ever read by the valuation engine. The only path from an observation to a
    /// benchmark runs through the derivation engine and an admin pressing Add on the existing form.
    /// </summary>
    public sealed class BenchmarkObservation : Entity
    {
        public Guid Id { get; set; }

        public BenchmarkObservationSource Source { get; set; }

        /// <summary>Issuer the fact belongs to; <c>null</c> for sector-level (Damodaran) observations.</summary>
        public Guid? IssuerId { get; set; }

        /// <summary>Damodaran bucket name; <c>null</c> for issuer-level observations.</summary>
        public string? ExternalKey { get; set; }

        public BenchmarkObservationMetric Metric { get; set; }

        public decimal Value { get; set; }

        /// <summary>The date the value describes — quarter start for a market cap, dataset year for Damodaran.</summary>
        public DateTime AsOf { get; set; }

        /// <summary>
        /// Fiscal year of a <see cref="BenchmarkObservationMetric.Revenue"/> figure. Kept separate from
        /// <see cref="AsOf"/> because ГИР БО publishes year N in the middle of N+1: the multiple is
        /// unavoidably "today's price over the year-before-last's revenue", and the derivation has to
        /// be able to say so out loud.
        /// </summary>
        public int? FiscalYear { get; set; }

        /// <summary>Regional slice of the Damodaran dataset ("Emerging Markets", "Global"); <c>null</c> otherwise.</summary>
        public string? DatasetRegion { get; set; }

        public DateTime FetchedAt { get; set; }

        /// <summary>Free-text provenance marker, e.g. "manual override".</summary>
        public string? OriginNote { get; set; }

        public BenchmarkObservation() { }

        public static BenchmarkObservation ForIssuer(
            BenchmarkObservationSource source,
            Guid issuerId,
            BenchmarkObservationMetric metric,
            decimal value,
            DateTime asOf,
            int? fiscalYear,
            string? originNote,
            DateTime fetchedAt)
            => new()
            {
                Id = Guid.NewGuid(),
                Source = source,
                IssuerId = issuerId,
                Metric = metric,
                Value = value,
                AsOf = asOf,
                FiscalYear = fiscalYear,
                OriginNote = originNote,
                FetchedAt = fetchedAt,
            };

        public static BenchmarkObservation ForBucket(
            BenchmarkObservationSource source,
            string externalKey,
            BenchmarkObservationMetric metric,
            decimal value,
            DateTime asOf,
            string? datasetRegion,
            string? originNote,
            DateTime fetchedAt)
            => new()
            {
                Id = Guid.NewGuid(),
                Source = source,
                ExternalKey = externalKey,
                Metric = metric,
                Value = value,
                AsOf = asOf,
                DatasetRegion = datasetRegion,
                OriginNote = originNote,
                FetchedAt = fetchedAt,
            };

        /// <summary>Refreshes an existing row in place — the upsert half of the staging contract.</summary>
        public void Refresh(decimal value, int? fiscalYear, string? datasetRegion, string? originNote, DateTime fetchedAt)
        {
            Value = value;
            FiscalYear = fiscalYear;
            DatasetRegion = datasetRegion;
            OriginNote = originNote;
            FetchedAt = fetchedAt;
        }
    }
}
