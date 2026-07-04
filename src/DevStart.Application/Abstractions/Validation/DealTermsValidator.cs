using DevStart.Domain.InvestmentApplications;

namespace DevStart.Application.Abstractions.Validation
{
    internal sealed class DealTermsValidator : IDealTermsValidator
    {
        private const string SeverityWarning = "warning";

        public IReadOnlyList<DealTermsFlag> Validate(DealTermsInput input)
        {
            List<DealTermsFlag> flags = new();

            if (input.Discount.HasValue && input.Discount.Value > 0.25m)
            {
                flags.Add(new DealTermsFlag(
                    "deal_terms.aggressive_discount",
                    SeverityWarning,
                    "Discount above 25% is aggressive by market standards."));
            }

            if (input.LiquidationPreference > 1.0m)
            {
                flags.Add(new DealTermsFlag(
                    "deal_terms.high_liq_pref",
                    SeverityWarning,
                    "Liquidation preference above 1x reduces founders' upside on exit."));
            }

            if (input.Instrument == InvestmentInstrument.ConvertibleLoan
                && input.InterestRate.HasValue
                && input.InterestRate.Value > 0.08m)
            {
                flags.Add(new DealTermsFlag(
                    "deal_terms.high_interest_rate",
                    SeverityWarning,
                    "Interest rate above 8% is above the typical market range for convertible notes."));
            }

            // For cap-based instruments the invested amount must stay below the cap, otherwise the
            // implied share is >= 100% and gets clamped, leaving founders with nothing.
            if ((input.Instrument == InvestmentInstrument.Safe
                    || input.Instrument == InvestmentInstrument.ConvertibleLoan)
                && input.ValuationCap.HasValue
                && input.ValuationCap.Value > 0
                && input.Amount >= input.ValuationCap.Value)
            {
                flags.Add(new DealTermsFlag(
                    "deal_terms.amount_exceeds_cap",
                    SeverityWarning,
                    "Invested amount meets or exceeds the valuation cap; the investor would take the entire cap table."));
            }

            // Implied investor share via the shared formula (InvestorShareMath) — the same math the
            // cap table uses, including accrued convertible-note interest.
            decimal? investorShare = ComputeShare(input);
            if (investorShare.HasValue && investorShare.Value > 0.30m)
            {
                flags.Add(new DealTermsFlag(
                    "deal_terms.high_dilution",
                    SeverityWarning,
                    "Investor share above 30% is unusually high for an early-stage round."));
            }

            return flags;
        }

        private static decimal? ComputeShare(DealTermsInput input) =>
            InvestorShareMath.ComputeShareFraction(
                input.Instrument,
                input.Amount,
                input.ValuationCap,
                input.InterestRate,
                input.TermMonths,
                input.PreMoneyValuation);
    }
}
