using DevStart.Domain.StartupPatents;
using DevStart.SharedKernel;

namespace DevStart.Domain.PatentRegistry
{
    /// <summary>
    /// One row of the local copy of the Rospatent open-data register. Loading is an upsert keyed by
    /// (kind, normalized number) — a lapsed record stays in the dump with a changed status, so nothing
    /// is ever deleted and a partial load means "some rows are stale", not "the register is broken".
    ///
    /// The rightsholder is stored as the dump gives it: a name string and, when the dump carries one,
    /// an INN. Neither is a statement about any startup — the comparison against a declared INN
    /// happens at read time and is reported as a comparison (SC-66).
    /// </summary>
    public sealed class PatentRegistryEntry : Entity
    {
        public Guid Id { get; set; }
        public IntellectualPropertyKind Kind { get; set; }

        /// <summary>Digits only, normalized the same way as a claimed number (<see cref="StartupPatent.NormalizeNumber"/>).</summary>
        public string NumberNormalized { get; set; } = null!;

        public string? Title { get; set; }

        /// <summary>Rightsholder as the dump names it. Free text — never parsed into an identity.</summary>
        public string? HolderName { get; set; }

        /// <summary>
        /// Rightsholder's INN when the dump carries one. Open data does not always publish it; a null
        /// here is the reason a record can resolve and still not be comparable to a declared INN.
        /// </summary>
        public string? HolderInn { get; set; }

        public DateOnly? RegisteredOn { get; set; }

        public PatentProtectionStatus Status { get; set; }

        /// <summary>Which load produced this row — dataset name or URL, for tracing a number back.</summary>
        public string? SourceNote { get; set; }

        public DateTime FetchedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public PatentRegistryEntry() { }

        public static PatentRegistryEntry Create(
            IntellectualPropertyKind kind,
            string numberNormalized,
            string? title,
            string? holderName,
            string? holderInn,
            DateOnly? registeredOn,
            PatentProtectionStatus status,
            string? sourceNote,
            DateTime fetchedAt)
            => new()
            {
                Id = Guid.NewGuid(),
                Kind = kind,
                NumberNormalized = numberNormalized,
                Title = title,
                HolderName = holderName,
                HolderInn = holderInn,
                RegisteredOn = registeredOn,
                Status = status,
                SourceNote = sourceNote,
                FetchedAt = fetchedAt,
                UpdatedAt = fetchedAt
            };

        /// <summary>Applies a newer load over an existing row. The key (kind, number) never changes.</summary>
        public void Refresh(
            string? title,
            string? holderName,
            string? holderInn,
            DateOnly? registeredOn,
            PatentProtectionStatus status,
            string? sourceNote,
            DateTime fetchedAt)
        {
            Title = title;
            HolderName = holderName;
            HolderInn = holderInn;
            RegisteredOn = registeredOn;
            Status = status;
            SourceNote = sourceNote;
            FetchedAt = fetchedAt;
            UpdatedAt = fetchedAt;
        }
    }
}
