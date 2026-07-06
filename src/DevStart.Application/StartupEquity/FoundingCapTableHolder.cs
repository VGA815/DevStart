using DevStart.Domain.StartupEquity;

namespace DevStart.Application.StartupEquity
{
    /// <summary>
    /// A resolved row of a startup's founding (pre-money) cap table: either a persisted
    /// <see cref="StartupEquityHolder"/> or a bootstrapped default. <see cref="Name"/> is always
    /// populated (founder names resolved from their Profile). This is the single shape both the
    /// cap-table read model and the term-sheet generator build upon.
    /// </summary>
    public sealed record FoundingCapTableHolder(
        Guid? ProfileId,
        EquityHolderType HolderType,
        string Name,
        decimal EquityPercentage,
        DateTime? VestingStartDate,
        int? VestingMonths,
        int? CliffMonths);
}
