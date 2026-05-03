using DevStart.Application.Scoring;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.Startups;

namespace DevStart.Application.DealDocuments.Generation
{
    public interface ITermSheetGenerator
    {
        /// <summary>
        /// Loads the appropriate markdown template from MinIO and renders it by replacing all
        /// {{placeholder}} tokens with values derived from the deal, score, and cap table.
        /// </summary>
        Task<string> RenderAsync(
            InvestmentDeal deal,
            Startup startup,
            ScoreResult score,
            CapTableResult capTable,
            CancellationToken cancellationToken);
    }
}
