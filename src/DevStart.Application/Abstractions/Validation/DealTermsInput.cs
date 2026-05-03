using DevStart.Domain.InvestmentApplications;

namespace DevStart.Application.Abstractions.Validation
{
    public sealed record DealTermsInput(
        InvestmentInstrument Instrument,
        decimal Amount,
        decimal? ValuationCap,
        decimal? Discount,
        decimal? InterestRate,
        int? TermMonths,
        decimal? PreMoneyValuation,
        decimal LiquidationPreference,
        bool ProRataRights);
}
