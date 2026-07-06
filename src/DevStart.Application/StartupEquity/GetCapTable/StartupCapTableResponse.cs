using DevStart.Domain.StartupEquity;

namespace DevStart.Application.StartupEquity.GetCapTable
{
    public sealed class StartupCapTableResponse
    {
        public Guid StartupId { get; set; }

        /// <summary>False when no explicit cap table has been saved yet and this is the bootstrapped default.</summary>
        public bool IsConfigured { get; set; }

        public decimal TotalPercentage { get; set; }

        public decimal TotalVestedPercentage { get; set; }

        public List<StartupCapTableHolderResponse> Holders { get; set; } = [];
    }

    public sealed class StartupCapTableHolderResponse
    {
        public Guid? ProfileId { get; set; }
        public EquityHolderType HolderType { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal EquityPercentage { get; set; }

        public DateTime? VestingStartDate { get; set; }
        public int? VestingMonths { get; set; }
        public int? CliffMonths { get; set; }

        /// <summary>Fraction of this holder's equity vested as of now, in <c>[0, 1]</c>.</summary>
        public decimal VestedFraction { get; set; }

        /// <summary>Vested share as a percent of the whole company (<c>EquityPercentage × VestedFraction</c>).</summary>
        public decimal VestedPercentage { get; set; }
    }
}
