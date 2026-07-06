using DevStart.Application.Abstractions.Validation;
using DevStart.Domain.InvestmentDeals;

namespace DevStart.Application.DealDocuments.Generation
{
    internal sealed class CapTableCalculator : ICapTableCalculator
    {
        private const string SeverityWarning = "warning";
        private const string SeverityInfo = "info";
        private const decimal FoundersFloorPct = 40m;
        private const decimal InvestorCeilingPct = 30m;

        public CapTableResult Compute(InvestmentDeal deal, IReadOnlyList<EquityHolderInput> holdersBefore)
        {
            decimal rawShareFraction = ComputeInvestorShareFraction(deal);
            // >= 1: at amount == cap the investor takes exactly 100% and founders are wiped —
            // that deserves the warning just as much as overshooting the cap.
            bool shareCapped = rawShareFraction >= 1m;

            decimal investorShareFraction = rawShareFraction;
            if (investorShareFraction < 0m) investorShareFraction = 0m;
            if (investorShareFraction > 1m) investorShareFraction = 1m;

            decimal investorSharePct = Math.Round(investorShareFraction * 100m, 2, MidpointRounding.AwayFromZero);
            decimal dilutionFactor = 1m - investorShareFraction;

            List<CapTableEntry> entries = new(holdersBefore.Count + 1);
            // Vested fraction per entry, kept parallel to `entries` so VestedPctAfter can be recomputed
            // after the after-column is normalized. The investor's stake is fully vested (fraction 1).
            List<decimal> vestedFractions = new(holdersBefore.Count + 1);
            foreach (EquityHolderInput holder in holdersBefore)
            {
                decimal pctAfter = Math.Round(holder.SharePct * dilutionFactor, 2, MidpointRounding.AwayFromZero);
                entries.Add(new CapTableEntry(
                    PartyId: holder.PartyId,
                    PartyName: holder.Name,
                    PartyType: holder.Type,
                    SharePctBefore: holder.SharePct,
                    SharePctAfter: pctAfter));
                vestedFractions.Add(ClampFraction(holder.VestedFraction));
            }

            entries.Add(new CapTableEntry(
                PartyId: deal.InvestorProfileId,
                PartyName: "New Investor",
                PartyType: "Investor",
                SharePctBefore: 0m,
                SharePctAfter: investorSharePct));
            vestedFractions.Add(1m);

            // Each row's after-share is rounded independently, so the column can drift off 100%
            // (e.g. 99.99% / 100.02%). Absorb that rounding residual into the largest holder so the
            // cap table presented in the term sheet always totals exactly 100%.
            NormalizeAfterColumn(entries);

            // Vested amounts derive from the final (normalized) after-share, so compute them last.
            for (int i = 0; i < entries.Count; i++)
            {
                decimal vestedPctAfter = Math.Round(
                    entries[i].SharePctAfter * vestedFractions[i], 2, MidpointRounding.AwayFromZero);
                entries[i] = entries[i] with { VestedPctAfter = vestedPctAfter };
            }

            // Recompute headline figures from the normalized entries to keep them consistent.
            investorSharePct = entries[^1].SharePctAfter;
            decimal foundersTotalAfter = entries
                .Where(e => string.Equals(e.PartyType, "Founder", StringComparison.OrdinalIgnoreCase))
                .Sum(e => e.SharePctAfter);
            decimal foundersUnvestedAfter = entries
                .Where(e => string.Equals(e.PartyType, "Founder", StringComparison.OrdinalIgnoreCase))
                .Sum(e => e.SharePctAfter - e.VestedPctAfter);

            List<DealTermsFlag> warnings = new();
            if (shareCapped)
            {
                warnings.Add(new DealTermsFlag(
                    "cap_table.share_capped",
                    SeverityWarning,
                    "Invested amount meets or exceeds the valuation cap; the investor share was capped at 100%. Review the deal terms."));
            }
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
            if (foundersUnvestedAfter > 0.01m)
            {
                warnings.Add(new DealTermsFlag(
                    "cap_table.founder_unvested",
                    SeverityInfo,
                    $"Founders hold {foundersUnvestedAfter:0.##}% of equity that is not yet vested as of the term-sheet date; this is subject to their vesting schedules."));
            }

            return new CapTableResult(
                Entries: entries,
                InvestorSharePct: investorSharePct,
                FoundersTotalAfterPct: Math.Round(foundersTotalAfter, 2, MidpointRounding.AwayFromZero),
                Warnings: warnings);
        }

        private static decimal ClampFraction(decimal fraction)
        {
            if (fraction < 0m) return 0m;
            if (fraction > 1m) return 1m;
            return fraction;
        }

        // Largest-remainder normalization: nudge the biggest holder by the rounding residual so the
        // after-column sums to exactly 100%. Guarded to a small residual so it only corrects rounding
        // drift and never masks a genuinely degenerate cap table.
        private static void NormalizeAfterColumn(List<CapTableEntry> entries)
        {
            if (entries.Count == 0)
            {
                return;
            }

            decimal sum = entries.Sum(e => e.SharePctAfter);
            decimal residual = Math.Round(100m - sum, 2, MidpointRounding.AwayFromZero);
            if (residual == 0m || Math.Abs(residual) > 1m)
            {
                return;
            }

            int largestIndex = 0;
            for (int i = 1; i < entries.Count; i++)
            {
                if (entries[i].SharePctAfter > entries[largestIndex].SharePctAfter)
                {
                    largestIndex = i;
                }
            }

            CapTableEntry largest = entries[largestIndex];
            entries[largestIndex] = largest with
            {
                SharePctAfter = Math.Round(largest.SharePctAfter + residual, 2, MidpointRounding.AwayFromZero)
            };
        }

        private static decimal ComputeInvestorShareFraction(InvestmentDeal deal) =>
            InvestorShareMath.ComputeShareFraction(
                deal.Instrument,
                deal.Amount,
                deal.ValuationCap,
                deal.InterestRate,
                deal.TermMonths,
                deal.PreMoneyValuation) ?? 0m;
    }
}
