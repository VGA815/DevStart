using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Valuation;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Infrastructure.Valuation
{
    internal sealed class BenchmarkObservationStore(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider) : IBenchmarkObservationStore
    {
        public async Task UpsertIssuerObservationsAsync(
            IReadOnlyCollection<IssuerObservation> observations,
            CancellationToken cancellationToken)
        {
            if (observations.Count == 0)
            {
                return;
            }

            DateTime now = dateTimeProvider.UtcNow;

            HashSet<Guid> issuerIds = observations.Select(o => o.IssuerId).ToHashSet();
            DateTime earliest = observations.Min(o => o.AsOf);

            // One query for the whole run's existing rows instead of one per issuer. Staging is small
            // enough that the extra rows this may pull in cost nothing.
            List<BenchmarkObservation> existing = await context.BenchmarkObservations
                .Where(o => o.IssuerId != null
                    && issuerIds.Contains(o.IssuerId.Value)
                    && o.ExternalKey == null
                    && o.AsOf >= earliest)
                .ToListAsync(cancellationToken);

            Dictionary<(Guid, BenchmarkObservationSource, BenchmarkObservationMetric, DateTime), BenchmarkObservation> byKey =
                existing.ToDictionary(o => (o.IssuerId!.Value, o.Source, o.Metric, o.AsOf));

            foreach (IssuerObservation observation in observations)
            {
                var key = (observation.IssuerId, observation.Source, observation.Metric, observation.AsOf);

                if (byKey.TryGetValue(key, out BenchmarkObservation? row))
                {
                    row.Refresh(
                        observation.Value, observation.FiscalYear, datasetRegion: null,
                        observation.OriginNote, now);
                    continue;
                }

                BenchmarkObservation added = BenchmarkObservation.ForIssuer(
                    observation.Source, observation.IssuerId, observation.Metric, observation.Value,
                    observation.AsOf, observation.FiscalYear, observation.OriginNote, now);

                context.BenchmarkObservations.Add(added);

                // Guard the batch against duplicate keys within itself — two rows with the same key
                // would otherwise both be inserted and trip the unique index.
                byKey[key] = added;
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task ReplaceDamodaranYearAsync(
            int datasetYear,
            string datasetRegion,
            IReadOnlyCollection<DamodaranBucketObservation> buckets,
            CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;
            var asOf = new DateTime(datasetYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Scoped to (year, region), not to the year alone. A re-upload corrects the slice it names;
            // it must not take a different regional slice of the same year down with it, because the
            // derivation picks a slice by name and would then quietly find nothing.
            List<BenchmarkObservation> stale = await context.BenchmarkObservations
                .Where(o => o.Source == BenchmarkObservationSource.Damodaran
                    && o.AsOf == asOf
                    && o.DatasetRegion == datasetRegion)
                .ToListAsync(cancellationToken);

            context.BenchmarkObservations.RemoveRange(stale);

            foreach (DamodaranBucketObservation bucket in buckets)
            {
                context.BenchmarkObservations.Add(BenchmarkObservation.ForBucket(
                    BenchmarkObservationSource.Damodaran,
                    bucket.ExternalKey,
                    BenchmarkObservationMetric.EvSales,
                    bucket.EvSales,
                    asOf,
                    datasetRegion,
                    originNote: $"Damodaran {datasetYear} {datasetRegion}",
                    fetchedAt: now));
            }

            // One SaveChanges for the delete and the insert together: a half-replaced year is exactly
            // the "plausible but incomplete set" the all-or-nothing rule exists to prevent.
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
