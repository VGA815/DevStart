using DevStart.SharedKernel;

namespace DevStart.Domain.StartupEquity
{
    /// <summary>
    /// A single row of a startup's founding (pre-money) capitalization table: a founder, the ESOP
    /// pool, an advisor, or another holder. <see cref="EquityPercentage"/> is a share of the whole
    /// company and, across all holders of a startup, the rows sum to 100% (enforced at the command
    /// layer). Vesting is informational — it never changes <see cref="EquityPercentage"/>; the
    /// vested fraction is computed on a date by the vesting calculator.
    /// </summary>
    public sealed class StartupEquityHolder : Entity
    {
        public Guid Id { get; set; }
        public Guid StartupId { get; set; }

        /// <summary>Set for a founder (references the member's Profile == UserId); <c>null</c> for ESOP/advisor rows.</summary>
        public Guid? ProfileId { get; set; }

        public EquityHolderType HolderType { get; set; }

        /// <summary>Display name for non-personal rows (ESOP/advisor); founders resolve their name from the Profile.</summary>
        public string? Name { get; set; }

        /// <summary>Ownership as a percent of the whole company, pre-money (0..100).</summary>
        public decimal EquityPercentage { get; set; }

        /// <summary>Vesting commencement date; <c>null</c> means fully vested (no schedule).</summary>
        public DateTime? VestingStartDate { get; set; }

        /// <summary>Total vesting duration in months (e.g. 48); <c>null</c> means fully vested.</summary>
        public int? VestingMonths { get; set; }

        /// <summary>Cliff in months (e.g. 12); nothing vests before it. <c>null</c> ⇒ no cliff.</summary>
        public int? CliffMonths { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public StartupEquityHolder() { }

        public static StartupEquityHolder Create(
            Guid startupId,
            EquityHolderType holderType,
            Guid? profileId,
            string? name,
            decimal equityPercentage,
            DateTime? vestingStartDate,
            int? vestingMonths,
            int? cliffMonths,
            DateTime utcNow)
            => new()
            {
                Id = Guid.NewGuid(),
                StartupId = startupId,
                HolderType = holderType,
                ProfileId = profileId,
                Name = name,
                EquityPercentage = equityPercentage,
                VestingStartDate = vestingStartDate,
                VestingMonths = vestingMonths,
                CliffMonths = cliffMonths,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };
    }
}
