using DevStart.Domain.InvestmentDeals;

namespace DevStart.Application.DealDocuments.Generation
{
    public interface ICapTableCalculator
    {
        /// <summary>
        /// Computes a snapshot cap table for a deal: each prior holder is diluted proportionally,
        /// the new investor is appended with the freshly-computed share. Warnings are emitted when
        /// founders' total share falls below 40% or the investor exceeds 30%.
        /// </summary>
        CapTableResult Compute(InvestmentDeal deal, IReadOnlyList<EquityHolderInput> holdersBefore);
    }
}
