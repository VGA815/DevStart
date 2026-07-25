using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Npd.GetIncomeStatus
{
    /// <summary>Admin view of the current НПД income position. <see cref="Year"/> defaults to the current МСК year.</summary>
    public sealed record GetNpdIncomeStatusQuery(int? Year = null) : IQuery<NpdIncomeStatusResponse>;

    public sealed class NpdIncomeStatusResponse
    {
        public int Year { get; init; }
        public decimal IncomeToDate { get; init; }
        public decimal Limit { get; init; }
        public decimal WarningAmount { get; init; }
        public decimal Remaining { get; init; }
        public bool WarningReached { get; init; }
        public bool LimitReached { get; init; }
    }
}
