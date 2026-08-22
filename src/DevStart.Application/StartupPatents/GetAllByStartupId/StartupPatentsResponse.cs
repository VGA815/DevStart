using DevStart.Application.Abstractions.Registry;
using DevStart.Domain.PatentRegistry;
using DevStart.Domain.Registries;
using DevStart.Domain.StartupPatents;

namespace DevStart.Application.StartupPatents.GetAllByStartupId
{
    /// <summary>
    /// The startup's IP records next to the claim they stand beside. Both are shown: the reader sees
    /// "declared: yes" and the list of records, and judges the gap between them. The platform states
    /// what it knows and stops there — no wording here says ownership is confirmed.
    /// </summary>
    public sealed class StartupPatentsResponse
    {
        public Guid StartupId { get; init; }

        /// <summary>
        /// The <c>has_patents</c> checkbox. Kept as its own claim rather than derived from the record
        /// count: foreign patents and know-how are real IP with no Russian register entry.
        /// </summary>
        public bool HasPatentsDeclared { get; init; }

        /// <summary>ИНН the startup declared. A declaration — see <see cref="LegalEntity"/>.</summary>
        public string? DeclaredInn { get; init; }

        /// <summary>What ЕГРЮЛ says about that ИНН, including "the lookup is unavailable".</summary>
        public LegalEntityResponse? LegalEntity { get; init; }

        public List<StartupPatentResponse> Records { get; init; } = [];
    }

    public sealed class LegalEntityResponse
    {
        public LegalEntityLookupState State { get; init; }
        public string? Inn { get; init; }
        public string? Name { get; init; }
        public bool? IsActive { get; init; }
        public string? StatusText { get; init; }
        public DateOnly? AsOf { get; init; }
    }

    public sealed class StartupPatentResponse
    {
        public Guid Id { get; init; }
        public IntellectualPropertyKind Kind { get; init; }

        /// <summary>The number as the founder typed it.</summary>
        public string Number { get; init; } = null!;

        /// <summary>Digits only — what the register was searched by.</summary>
        public string NumberNormalized { get; init; } = null!;

        public RegistryLookupState State { get; init; }

        /// <summary>How the rightsholder's ИНН compares with the declared one. A comparison, not a proof.</summary>
        public DeclaredValueComparison Ownership { get; init; }

        public string? Title { get; init; }

        /// <summary>Rightsholder as the register names it, shown as is.</summary>
        public string? HolderName { get; init; }

        public string? HolderInn { get; init; }
        public DateOnly? RegisteredOn { get; init; }
        public PatentProtectionStatus? ProtectionStatus { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
