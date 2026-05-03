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

            // Investor share = amount / cap (Safe / Convertible) or amount / (premoney + amount) (Priced)
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

        private static decimal? ComputeShare(DealTermsInput input)
        {
            switch (input.Instrument)
            {
                case InvestmentInstrument.Safe:
                case InvestmentInstrument.ConvertibleLoan:
                    if (input.ValuationCap.HasValue && input.ValuationCap.Value > 0)
                    {
                        return input.Amount / input.ValuationCap.Value;
                    }
                    return null;

                case InvestmentInstrument.PricedRound:
                    if (input.PreMoneyValuation.HasValue && input.PreMoneyValuation.Value > 0)
                    {
                        return input.Amount / (input.PreMoneyValuation.Value + input.Amount);
                    }
                    return null;

                default:
                    return null;
            }
        }
    }
}
