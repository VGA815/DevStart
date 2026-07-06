using DevStart.Application.Scoring;
using DevStart.Application.StartupEquity;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.Startups;

namespace DevStart.Application.DealDocuments.Generation
{
    public interface ITermSheetGenerator
    {
        /// <summary>
        /// Loads the appropriate markdown template from MinIO and renders it by replacing all
        /// {{placeholder}} tokens with values derived from the deal, score, and cap table. The
        /// founding holders (with their vesting schedules) drive the per-founder breakdown section;
        /// <paramref name="asOf"/> is the date vested amounts are computed against.
        /// </summary>
        Task<string> RenderAsync(
            InvestmentDeal deal,
            Startup startup,
            ScoreResult score,
            CapTableResult capTable,
            IReadOnlyList<FoundingCapTableHolder> foundingHolders,
            DateTime asOf,
            CancellationToken cancellationToken);
    }
}
