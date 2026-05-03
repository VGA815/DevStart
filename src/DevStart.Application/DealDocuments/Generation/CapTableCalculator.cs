using DevStart.Application.Abstractions.Validation;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.InvestmentDeals;

namespace DevStart.Application.DealDocuments.Generation
{
    internal sealed class CapTableCalculator : ICapTableCalculator
    {
        private const string SeverityWarning = "warning";
        private const decimal FoundersFloorPct = 40m;
        private const decimal InvestorCeilingPct = 30m;

        public CapTableResult Compute(InvestmentDeal deal, IReadOnlyList<EquityHolderInput> holdersBefore)
        {
            decimal investorShareFraction = ComputeInvestorShareFraction(deal);
            if (investorShareFraction < 0m) investorShareFraction = 0m;
            if (investorShareFraction > 1m) investorShareFraction = 1m;

            decimal investorSharePct = Math.Round(investorShareFraction * 100m, 2, MidpointRounding.AwayFromZero);
            decimal dilutionFactor = 1m - investorShareFraction;

            List<CapTableEntry> entries = new(holdersBefore.Count + 1);
            foreach (EquityHolderInput holder in holdersBefore)
            {
                decimal pctAfter = Math.Round(holder.SharePct * dilutionFactor, 2, MidpointRounding.AwayFromZero);
                entries.Add(new CapTableEntry(
                    PartyId: holder.PartyId,
                    PartyName: holder.Name,
                    PartyType: holder.Type,
                    SharePctBefore: holder.SharePct,
                    SharePctAfter: pctAfter));
            }

            entries.Add(new CapTableEntry(
                PartyId: deal.InvestorProfileId,
                PartyName: "New Investor",
                PartyType: "Investor",
                SharePctBefore: 0m,
                SharePctAfter: investorSharePct));

            decimal foundersTotalAfter = entries
                .Where(e => string.Equals(e.PartyType, "Founder", StringComparison.OrdinalIgnoreCase))
                .Sum(e => e.SharePctAfter);

            List<DealTermsFlag> warnings = new();
            if (foundersTotalAfter < FoundersFloorPct)
            {
                warnings.Add(new DealTermsFlag(
                    "cap_table.founders_below_floor",
                    SeverityWarning,
                    $"Founders' combined share after the deal ({foundersTotalAfter:0.##}%) falls below the {FoundersFloorPct}% threshold."));
            }
            if (investorSharePct > InvestorCeilingPct)
            {
                warnings.Add(new DealTermsFlag(
                    "cap_table.investor_above_ceiling",
                    SeverityWarning,
                    $"New investor share ({investorSharePct:0.##}%) exceeds the {InvestorCeilingPct}% threshold."));
            }

            return new CapTableResult(
                Entries: entries,
                InvestorSharePct: investorSharePct,
                FoundersTotalAfterPct: Math.Round(foundersTotalAfter, 2, MidpointRounding.AwayFromZero),
                Warnings: warnings);
        }

        private static decimal ComputeInvestorShareFraction(InvestmentDeal deal)
        {
            switch (deal.Instrument)
            {
                case InvestmentInstrument.Safe:
                    {
                        if (deal.ValuationCap is null || deal.ValuationCap.Value <= 0)
                        {
                            return 0m;
                        }
                        return deal.Amount / deal.ValuationCap.Value;
                    }
                case InvestmentInstrument.ConvertibleLoan:
                    {
                        if (deal.ValuationCap is null || deal.ValuationCap.Value <= 0)
                        {
                            return 0m;
                        }
                        decimal interest = (deal.InterestRate ?? 0m) * (deal.TermMonths ?? 0) / 12m;
                        decimal totalAtConversion = deal.Amount + (deal.Amount * interest);
                        return totalAtConversion / deal.ValuationCap.Value;
                    }
                case InvestmentInstrument.PricedRound:
                    {
                        if (deal.PreMoneyValuation is null || deal.PreMoneyValuation.Value <= 0)
                        {
                            return 0m;
                        }
                        return deal.Amount / (deal.PreMoneyValuation.Value + deal.Amount);
                    }
                default:
                    return 0m;
            }
        }
    }
}
