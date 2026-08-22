using DevStart.Domain.StartupPartnerships;

namespace DevStart.Application.StartupPartnerships.GetAllByStartupId
{
    public sealed class StartupPartnershipResponse
    {
        public Guid Id { get; init; }
        public Guid StartupId { get; init; }
        public string PartnerName { get; init; } = null!;
        public string Website { get; init; } = null!;
        public PartnershipKind Kind { get; init; }
        public string? Description { get; init; }

        /// <summary>
        /// Whether the record says what the arrangement actually is. Shipped rather than left for the
        /// client to re-derive from an empty description: it is the scoring driver, and a client that
        /// guessed the rule differently would explain the valuation wrong.
        /// </summary>
        public bool IsWorkedOut { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
