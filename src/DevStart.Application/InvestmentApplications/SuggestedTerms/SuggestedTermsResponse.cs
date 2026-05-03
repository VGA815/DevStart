using DevStart.Domain.InvestmentApplications;

namespace DevStart.Application.InvestmentApplications.SuggestedTerms
{
    public sealed class SuggestedTermsResponse
    {
        public Guid StartupId { get; init; }
        public InvestmentInstrument Instrument { get; init; }
        public decimal? SuggestedValuationCap { get; init; }
        public decimal? SuggestedDiscount { get; init; }
        public decimal? SuggestedInterestRate { get; init; }
        public int? SuggestedTermMonths { get; init; }
        public decimal? SuggestedPreMoneyValuation { get; init; }
        public decimal SuggestedLiquidationPreference { get; init; }
        public decimal ScoreReference { get; init; }
        public decimal ValuationLowReference { get; init; }
        public decimal ValuationHighReference { get; init; }
    }
}
