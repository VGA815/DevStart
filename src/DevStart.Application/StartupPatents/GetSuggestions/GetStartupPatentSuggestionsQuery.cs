using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.PatentRegistry;
using DevStart.Domain.StartupPatents;

namespace DevStart.Application.StartupPatents.GetSuggestions
{
    /// <summary>
    /// The reverse lookup (SC-66): records the register already attributes to the ИНН the startup
    /// declared, minus the ones it has listed. Nearly free — the register is local, so this is the same
    /// query read from the other side — and it spares the founder retyping numbers by hand.
    /// Not cached: it is member-only and changes the moment a record is added.
    /// </summary>
    public sealed class GetStartupPatentSuggestionsQuery : IQuery<StartupPatentSuggestionsResponse>
    {
        public Guid StartupId { get; set; }

        public GetStartupPatentSuggestionsQuery(Guid startupId)
        {
            StartupId = startupId;
        }
    }

    public sealed class StartupPatentSuggestionsResponse
    {
        /// <summary>Upper bound on returned rows — a shortlist to pick from, not a data dump.</summary>
        public const int MaxSuggestions = 50;

        /// <summary>The ИНН the suggestions were looked up by, echoed so the reader sees the basis.</summary>
        public string? DeclaredInn { get; init; }

        public List<StartupPatentSuggestion> Suggestions { get; init; } = [];
    }

    public sealed class StartupPatentSuggestion
    {
        public IntellectualPropertyKind Kind { get; init; }
        public string Number { get; init; } = null!;
        public string? Title { get; init; }
        public string? HolderName { get; init; }
        public DateOnly? RegisteredOn { get; init; }
        public PatentProtectionStatus Status { get; init; }
    }
}
