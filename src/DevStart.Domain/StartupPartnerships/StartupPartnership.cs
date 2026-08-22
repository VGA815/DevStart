using DevStart.Domain.Common;
using DevStart.SharedKernel;

namespace DevStart.Domain.StartupPartnerships
{
    /// <summary>
    /// One strategic partnership the startup claims: <i>this</i> partner, this kind of arrangement,
    /// this is what it gives us. It replaces the <c>has_strategic_partnerships</c> checkbox rather than
    /// standing beside it (М3 in docs/scoring-inputs-plan.md).
    ///
    /// The "beside, not instead" shape the IP records use does not apply here, and the difference is
    /// worth stating. A patents checkbox has honest grounds a Russian register cannot show — a foreign
    /// patent, know-how — so the claim survives alongside the records and the reader judges the gap.
    /// Partnerships have no register at all: there is nothing to prop the flag up with and no gap to
    /// display, so a flag worth a whole Berkus ceiling for one click is simply removed.
    ///
    /// The record carries no verification state, for the same reason and by the same rule as
    /// <c>StartupPatent</c>: the platform states what it holds. What makes the claim checkable here is
    /// a human reader — the partner is named, has a website, and the record says what the arrangement
    /// actually is.
    /// </summary>
    public sealed class StartupPartnership : Entity
    {
        /// <summary>
        /// Upper bound on partnership records per startup. The score saturates at
        /// <see cref="SaturationCount"/>, far below this, so the limit is not a scoring device — it
        /// keeps the visible list readable, which is what makes the list checkable at all.
        /// </summary>
        public const int MaxPerStartup = 30;

        /// <summary>
        /// Worked-out records beyond which the Berkus partnerships factor stops growing. Three, the
        /// same rung count as the competitor-analysis ladder: past that, more records say more about
        /// the founder's typing than about the business.
        /// </summary>
        public const int SaturationCount = 3;

        public Guid Id { get; set; }
        public Guid StartupId { get; set; }

        /// <summary>Partner as the startup names it — shown to the reader as typed.</summary>
        public string PartnerName { get; set; } = null!;

        /// <summary>
        /// Partner's website. Mandatory: it is what lets a reader go and look, and it is where the
        /// per-startup dedup key comes from.
        /// </summary>
        public string Website { get; set; } = null!;

        /// <summary>
        /// Host of <see cref="Website"/>, lower-cased and stripped of a leading "www." — the dedup key
        /// within a startup, so one partner cannot be listed three times under three URLs.
        /// </summary>
        public string NormalizedDomain { get; set; } = null!;

        public PartnershipKind Kind { get; set; }

        /// <summary>
        /// What the arrangement actually is and what it gives the startup. Optional in storage and on
        /// write — and the single thing that separates a worked-out record from a placeholder. A record
        /// without it is listed, counted in the total, and worth exactly nothing to the score.
        /// </summary>
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Whether the record carries an actual account of the partnership. The scoring driver — the
        /// count of these, never the total — so adding an empty record is worth nothing and deleting
        /// one can never raise the score.
        /// </summary>
        public bool IsWorkedOut => !string.IsNullOrWhiteSpace(Description);

        public StartupPartnership() { }

        public static StartupPartnership Create(
            Guid startupId,
            string partnerName,
            string website,
            string normalizedDomain,
            PartnershipKind kind,
            string? description,
            DateTime createdAt)
            => new()
            {
                Id = Guid.NewGuid(),
                StartupId = startupId,
                PartnerName = partnerName.Trim(),
                Website = website.Trim(),
                NormalizedDomain = normalizedDomain,
                Kind = kind,
                Description = description,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

        public void Update(
            string partnerName,
            string website,
            string normalizedDomain,
            PartnershipKind kind,
            string? description,
            DateTime utcNow)
        {
            PartnerName = partnerName.Trim();
            Website = website.Trim();
            NormalizedDomain = normalizedDomain;
            Kind = kind;
            Description = description;
            UpdatedAt = utcNow;
        }

        /// <summary>
        /// Reduces the partner's website to its comparable domain. Same rule as the competitor cards
        /// use — see <see cref="WebsiteDomain"/>.
        /// </summary>
        public static string? NormalizeDomain(string? website) => WebsiteDomain.Normalize(website);
    }
}
