using DevStart.Domain.InvestmentApplications;

namespace DevStart.Application.Abstractions.Validation
{
    /// <summary>
    /// Single home for the implied investor-share formula, shared by the cap-table calculator,
    /// the deal-terms validator and the suggested-terms endpoint so they can never disagree
    /// (e.g. one accruing convertible-note interest and another not).
    /// Returns <c>null</c> when the required denominator is missing or non-positive.
    /// </summary>
    internal static class InvestorShareMath
    {
        internal static decimal? ComputeShareFraction(
            InvestmentInstrument instrument,
            decimal amount,
            decimal? valuationCap,
            decimal? interestRate,
            int? termMonths,
            decimal? preMoneyValuation)
        {
            switch (instrument)
            {
                case InvestmentInstrument.Safe:
                    if (valuationCap is not > 0m)
                    {
                        return null;
                    }
                    return amount / valuationCap.Value;

                case InvestmentInstrument.ConvertibleLoan:
                    if (valuationCap is not > 0m)
                    {
                        return null;
                    }
                    // Simple interest accrued over the note term converts together with the principal.
                    decimal interest = (interestRate ?? 0m) * (termMonths ?? 0) / 12m;
                    return amount * (1m + interest) / valuationCap.Value;

                case InvestmentInstrument.PricedRound:
                    if (preMoneyValuation is not > 0m)
                    {
                        return null;
                    }
                    return amount / (preMoneyValuation.Value + amount);

                default:
                    return null;
            }
        }
    }
}
