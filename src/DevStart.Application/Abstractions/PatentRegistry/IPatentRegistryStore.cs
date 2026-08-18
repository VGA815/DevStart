using DevStart.Application.PatentRegistry;

namespace DevStart.Application.Abstractions.PatentRegistry
{
    /// <summary>
    /// Writes parsed dump rows into the local register. The only write path there is — nothing else
    /// creates registry rows, and no user-facing action ever does.
    /// </summary>
    public interface IPatentRegistryStore
    {
        /// <summary>
        /// Upserts by (kind, normalized number). A lapsed record stays in the dump with a changed
        /// status, so nothing is deleted and a re-run of the same file changes nothing but timestamps —
        /// the load is idempotent by construction.
        /// </summary>
        Task<PatentRegistryUpsertResult> UpsertAsync(
            IReadOnlyCollection<PatentRegistryRecord> records,
            string? sourceNote,
            CancellationToken cancellationToken);
    }

    public sealed record PatentRegistryUpsertResult(int Inserted, int Updated)
    {
        public static readonly PatentRegistryUpsertResult Empty = new(0, 0);

        public int Total => Inserted + Updated;
    }
}
