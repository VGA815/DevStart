using DevStart.Domain.Startups;
using DevStart.SharedKernel;

namespace DevStart.Domain.Valuation
{
    /// <summary>
    /// One curated Russian public comparable: the bridge between an external issuer and an
    /// <see cref="Industry"/>. Everything the collection jobs need to know about a company lives here —
    /// the MOEX ticker for market cap, the INN for revenue, and the sector it is counted under.
    ///
    /// Unlike <see cref="ValuationBenchmark"/> this table is mutable, not append-only: it is the
    /// *instrument* that produces a figure, never the figure itself. Reproducibility rides on the
    /// derived row's <c>Source</c>, which records what it was derived from; versioning the instrument
    /// as well would duplicate that mechanic for nothing.
    /// </summary>
    public sealed class BenchmarkIssuer : Entity
    {
        public Guid Id { get; set; }

        /// <summary>MOEX SECID, e.g. "POSI". Unique; the market-cap job iterates these.</summary>
        public string Ticker { get; set; } = null!;

        /// <summary>INN of the legal entity queried in ГИР БО; <c>null</c> when revenue is override-only.</summary>
        public string? Inn { get; set; }

        public string DisplayName { get; set; } = null!;

        /// <summary>Sector this issuer is counted under when the median multiple is computed.</summary>
        public Industry Industry { get; set; }

        /// <summary>
        /// Delisting or a loss of comparability clears this flag. The row stays: observations already
        /// collected keep their foreign key, and the history of why it was once included survives.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Consolidated (IFRS, group-level) revenue entered by hand, in RUB. ГИР БО serves РСБУ for a
        /// single legal entity while the MOEX capitalisation is the whole group — for a holding the two
        /// can differ by multiples, and dividing them yields a meaningless number. When set, this wins
        /// over the collected figure and the observation is flagged as manual.
        /// </summary>
        public decimal? RevenueOverride { get; set; }

        /// <summary>Fiscal year the <see cref="RevenueOverride"/> belongs to. Required whenever the override is set.</summary>
        public int? RevenueOverrideFiscalYear { get; set; }

        /// <summary>Where the manual figure came from (report, page). Required whenever the override is set.</summary>
        public string? RevenueOverrideNote { get; set; }

        /// <summary>Why this issuer sits in this sector — the argument a later reviewer needs.</summary>
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public BenchmarkIssuer() { }

        public static BenchmarkIssuer Create(
            string ticker,
            string? inn,
            string displayName,
            Industry industry,
            string? note,
            DateTime utcNow)
            => new()
            {
                Id = Guid.NewGuid(),
                Ticker = ticker,
                Inn = inn,
                DisplayName = displayName,
                Industry = industry,
                IsActive = true,
                Note = note,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            };

        public void Update(
            string? inn,
            string displayName,
            Industry industry,
            bool isActive,
            decimal? revenueOverride,
            int? revenueOverrideFiscalYear,
            string? revenueOverrideNote,
            string? note,
            DateTime utcNow)
        {
            Inn = inn;
            DisplayName = displayName;
            Industry = industry;
            IsActive = isActive;
            RevenueOverride = revenueOverride;
            RevenueOverrideFiscalYear = revenueOverrideFiscalYear;
            RevenueOverrideNote = revenueOverrideNote;
            Note = note;
            UpdatedAt = utcNow;
        }
    }
}
