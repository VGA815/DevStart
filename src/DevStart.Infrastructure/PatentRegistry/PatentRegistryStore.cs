using DevStart.Application.Abstractions.PatentRegistry;
using DevStart.Application.PatentRegistry;
using DevStart.Domain.PatentRegistry;
using DevStart.Domain.StartupPatents;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Infrastructure.PatentRegistry
{
    internal sealed class PatentRegistryStore(
        ApplicationDbContext context,
        IDateTimeProvider dateTimeProvider) : IPatentRegistryStore
    {
        /// <summary>
        /// Rows per transaction. A register dump runs to hundreds of thousands of rows, so the load is
        /// chunked: one failed chunk leaves the earlier ones applied, which is the intended behaviour —
        /// a partial load means "some rows are stale", never "the register is broken".
        /// </summary>
        private const int ChunkSize = 1000;

        public async Task<PatentRegistryUpsertResult> UpsertAsync(
            IReadOnlyCollection<PatentRegistryRecord> records,
            string? sourceNote,
            CancellationToken cancellationToken)
        {
            if (records.Count == 0)
            {
                return PatentRegistryUpsertResult.Empty;
            }

            DateTime now = dateTimeProvider.UtcNow;
            int inserted = 0;
            int updated = 0;

            foreach (PatentRegistryRecord[] chunk in records.Chunk(ChunkSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<IntellectualPropertyKind> kinds = chunk.Select(r => r.Kind).Distinct().ToList();
                List<string> numbers = chunk.Select(r => r.NumberNormalized).Distinct().ToList();

                List<PatentRegistryEntry> existing = await context.PatentRegistryEntries
                    .Where(e => kinds.Contains(e.Kind) && numbers.Contains(e.NumberNormalized))
                    .ToListAsync(cancellationToken);

                Dictionary<(IntellectualPropertyKind, string), PatentRegistryEntry> byKey =
                    existing.ToDictionary(e => (e.Kind, e.NumberNormalized));

                foreach (PatentRegistryRecord record in chunk)
                {
                    var key = (record.Kind, record.NumberNormalized);

                    if (byKey.TryGetValue(key, out PatentRegistryEntry? row))
                    {
                        row.Refresh(
                            record.Title, record.HolderName, record.HolderInn,
                            record.RegisteredOn, record.Status, sourceNote, now);
                        updated++;
                        continue;
                    }

                    PatentRegistryEntry added = PatentRegistryEntry.Create(
                        record.Kind, record.NumberNormalized, record.Title, record.HolderName,
                        record.HolderInn, record.RegisteredOn, record.Status, sourceNote, now);

                    context.PatentRegistryEntries.Add(added);
                    inserted++;

                    // Guard the chunk against duplicates within itself — two rows with the same key
                    // would otherwise both be inserted and trip the unique index.
                    byKey[key] = added;
                }

                await context.SaveChangesAsync(cancellationToken);

                // The tracker is cleared between chunks: a quarter-million tracked entities would turn
                // every subsequent SaveChanges into a scan of everything loaded so far.
                context.ChangeTracker.Clear();
            }

            return new PatentRegistryUpsertResult(inserted, updated);
        }
    }
}
