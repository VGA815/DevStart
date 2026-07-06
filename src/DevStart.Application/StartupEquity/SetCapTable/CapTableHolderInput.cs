using DevStart.Domain.StartupEquity;

namespace DevStart.Application.StartupEquity.SetCapTable
{
    /// <summary>One holder in a <see cref="SetStartupCapTableCommand"/>. For a founder, set
    /// <see cref="ProfileId"/> to the founder member's profile; for ESOP/advisor rows, set
    /// <see cref="Name"/> instead. Vesting fields are optional (all-or-nothing).</summary>
    public sealed record CapTableHolderInput(
        EquityHolderType HolderType,
        Guid? ProfileId,
        string? Name,
        decimal EquityPercentage,
        DateTime? VestingStartDate,
        int? VestingMonths,
        int? CliffMonths);
}
