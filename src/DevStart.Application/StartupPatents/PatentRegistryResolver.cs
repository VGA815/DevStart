using DevStart.Application.Abstractions.Data;
using DevStart.Domain.PatentRegistry;
using DevStart.Domain.StartupPatents;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupPatents
{
    internal sealed class PatentRegistryResolver(IApplicationDbContext context) : IPatentRegistryResolver
    {
        public async Task<StartupPatentResolution> ResolveAsync(Guid startupId, CancellationToken cancellationToken)
        {
            string? declaredInn = await context.Startups
                .AsNoTracking()
                .Where(s => s.Id == startupId)
                .Select(s => s.Inn)
                .FirstOrDefaultAsync(cancellationToken);

            List<StartupPatent> records = await context.StartupPatents
                .AsNoTracking()
                .Where(p => p.StartupId == startupId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

            if (records.Count == 0)
            {
                return new StartupPatentResolution(declaredInn, []);
            }

            List<IntellectualPropertyKind> kinds = records.Select(p => p.Kind).Distinct().ToList();
            List<string> numbers = records.Select(p => p.NumberNormalized).Distinct().ToList();

            List<PatentRegistryEntry> entries = await context.PatentRegistryEntries
                .AsNoTracking()
                .Where(e => kinds.Contains(e.Kind) && numbers.Contains(e.NumberNormalized))
                .ToListAsync(cancellationToken);

            Dictionary<(IntellectualPropertyKind, string), PatentRegistryEntry> byKey =
                entries.ToDictionary(e => (e.Kind, e.NumberNormalized));

            // "Not in the register" and "this part of the register was never loaded" are different
            // statements — the first is about the record, the second about the platform — so
            // availability is decided per kind rather than for the table as a whole. A trademark stays
            // uncheckable while only patents and programs are loaded, and says so.
            //
            // Cost matters here: this runs on every read of the tab against a table of hundreds of
            // thousands of rows. A kind that already matched is loaded by definition and needs no
            // query at all, so only the kinds with no match are probed — and each probe is a single
            // row taken off the (kind, number) index, the whole set of them sent as one UNION ALL.
            // A `DISTINCT kind` over the register would instead walk every row of it (PostgreSQL has
            // no index skip scan) to answer a question that six one-row lookups answer.
            HashSet<IntellectualPropertyKind> loadedKinds = entries.Select(e => e.Kind).ToHashSet();

            List<IntellectualPropertyKind> unmatchedKinds = kinds
                .Where(k => !loadedKinds.Contains(k))
                .ToList();

            if (unmatchedKinds.Count > 0)
            {
                IQueryable<IntellectualPropertyKind>? probe = null;
                foreach (IntellectualPropertyKind kind in unmatchedKinds)
                {
                    IQueryable<IntellectualPropertyKind> one = context.PatentRegistryEntries
                        .AsNoTracking()
                        .Where(e => e.Kind == kind)
                        .Select(e => e.Kind)
                        .Take(1);

                    probe = probe is null ? one : probe.Concat(one);
                }

                foreach (IntellectualPropertyKind kind in await probe!.ToListAsync(cancellationToken))
                {
                    loadedKinds.Add(kind);
                }
            }

            var resolved = new List<ResolvedStartupPatent>(records.Count);
            foreach (StartupPatent record in records)
            {
                if (!loadedKinds.Contains(record.Kind))
                {
                    resolved.Add(Unresolved(record, PatentResolutionState.RegistryUnavailable));
                    continue;
                }

                if (!byKey.TryGetValue((record.Kind, record.NumberNormalized), out PatentRegistryEntry? entry))
                {
                    resolved.Add(Unresolved(record, PatentResolutionState.NotFoundInRegistry));
                    continue;
                }

                resolved.Add(new ResolvedStartupPatent(
                    record.Id,
                    record.Kind,
                    record.NumberRaw,
                    record.NumberNormalized,
                    record.CreatedAt,
                    PatentResolutionState.Found,
                    Compare(declaredInn, entry.HolderInn),
                    entry.Title,
                    entry.HolderName,
                    entry.HolderInn,
                    entry.RegisteredOn,
                    entry.Status));
            }

            return new StartupPatentResolution(declaredInn, resolved);
        }

        public async Task<bool> HasRegistryCheckedOwnershipAsync(
            Guid startupId, string? declaredInn, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(declaredInn))
            {
                return false;
            }

            var records = await context.StartupPatents
                .AsNoTracking()
                .Where(p => p.StartupId == startupId)
                .Select(p => new { p.Kind, p.NumberNormalized })
                .ToListAsync(cancellationToken);

            if (records.Count == 0)
            {
                return false;
            }

            List<IntellectualPropertyKind> kinds = records.Select(p => p.Kind).Distinct().ToList();
            List<string> numbers = records.Select(p => p.NumberNormalized).Distinct().ToList();

            var candidates = await context.PatentRegistryEntries
                .AsNoTracking()
                .Where(e => e.HolderInn == declaredInn
                    && kinds.Contains(e.Kind)
                    && numbers.Contains(e.NumberNormalized))
                .Select(e => new { e.Kind, e.NumberNormalized })
                .ToListAsync(cancellationToken);

            // The pairs are re-matched in memory rather than trusted from the two IN-lists: a record of
            // one kind must not be counted as checked because a *different* kind carries the same
            // digits. At most MaxPerStartup rows on each side, so the cost is nil.
            HashSet<(IntellectualPropertyKind, string)> matched =
                candidates.Select(c => (c.Kind, c.NumberNormalized)).ToHashSet();

            return records.Any(r => matched.Contains((r.Kind, r.NumberNormalized)));
        }

        private static ResolvedStartupPatent Unresolved(StartupPatent record, PatentResolutionState state) =>
            new(record.Id,
                record.Kind,
                record.NumberRaw,
                record.NumberNormalized,
                record.CreatedAt,
                state,
                PatentOwnershipComparison.NotComparable,
                Title: null,
                HolderName: null,
                HolderInn: null,
                RegisteredOn: null,
                ProtectionStatus: null);

        private static PatentOwnershipComparison Compare(string? declaredInn, string? holderInn)
        {
            if (string.IsNullOrEmpty(declaredInn) || string.IsNullOrEmpty(holderInn))
            {
                return PatentOwnershipComparison.NotComparable;
            }

            return string.Equals(declaredInn, holderInn, StringComparison.Ordinal)
                ? PatentOwnershipComparison.MatchesDeclaredInn
                : PatentOwnershipComparison.DiffersFromDeclaredInn;
        }
    }
}
