using DevStart.Application.Scoring;
using DevStart.Application.StartupEquity;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.Startups;

namespace DevStart.Application.DealDocuments.Generation
{
    /// <summary>
    /// Turns the deal and everything computed around it into a <see cref="TermSheetModel"/>. This is
    /// where decisions about the document's *content* are made — most importantly whether the score
    /// block is showable at all — so that every renderer starts from the same answers.
    /// </summary>
    public interface ITermSheetComposer
    {
        /// <param name="asOf">The date vested amounts are computed against.</param>
        TermSheetModel Compose(
            InvestmentDeal deal,
            Startup startup,
            ScoreResult score,
            CapTableResult capTable,
            IReadOnlyList<FoundingCapTableHolder> foundingHolders,
            DateTime asOf);
    }
}
