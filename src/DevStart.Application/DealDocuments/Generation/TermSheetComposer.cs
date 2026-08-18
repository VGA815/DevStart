using DevStart.Application.Abstractions.Validation;
using DevStart.Application.Scoring;
using DevStart.Application.StartupEquity;
using DevStart.Application.StartupEquity.Vesting;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.StartupEquity;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;

namespace DevStart.Application.DealDocuments.Generation
{
    internal sealed class TermSheetComposer(
        IVestingCalculator vestingCalculator,
        IDateTimeProvider dateTimeProvider) : ITermSheetComposer
    {
        public TermSheetModel Compose(
            InvestmentDeal deal,
            Startup startup,
            ScoreResult score,
            CapTableResult capTable,
            IReadOnlyList<FoundingCapTableHolder> foundingHolders,
            DateTime asOf) =>
            new(
                Instrument: deal.Instrument,
                StartupName: startup.Name,
                StartupStage: startup.Stage.ToString(),
                DealId: deal.Id,
                ApplicationId: deal.ApplicationId,
                Amount: deal.Amount,
                ValuationCap: deal.ValuationCap,
                DiscountFraction: deal.Discount,
                InterestRateFraction: deal.InterestRate,
                TermMonths: deal.TermMonths,
                PreMoneyValuation: deal.PreMoneyValuation,
                LiquidationPreference: deal.LiquidationPreference,
                ProRataRights: deal.ProRataRights,
                InvestorSharePct: capTable.InvestorSharePct,
                FoundersTotalAfterPct: capTable.FoundersTotalAfterPct,
                CapTable: [.. capTable.Entries.Select(ToRow)],
                Founders: [.. ComposeFounders(foundingHolders, asOf)],
                Warnings: [.. capTable.Warnings.Select(ToWarning)],
                Score: ComposeScore(score),
                AsOf: asOf,
                GeneratedAt: dateTimeProvider.UtcNow);

        private static TermSheetCapTableRow ToRow(CapTableEntry e) =>
            new(e.PartyName, e.PartyType, e.SharePctBefore, e.SharePctAfter, e.VestedPctAfter);

        private static TermSheetWarning ToWarning(DealTermsFlag w) => new(w.Code, w.Severity, w.Message);

        private IEnumerable<TermSheetFounder> ComposeFounders(
            IReadOnlyList<FoundingCapTableHolder> foundingHolders,
            DateTime asOf)
        {
            foreach (FoundingCapTableHolder f in foundingHolders.Where(h => h.HolderType == EquityHolderType.Founder))
            {
                // A schedule the calculator can act on is start date plus a positive length; anything
                // else means the founder is on the platform's standard vesting and no computed vested
                // amount exists to show. That is the same test the markdown generator applied before
                // this type existed, moved here so both renderers agree on which founders have numbers.
                bool hasSchedule = f.VestingStartDate is not null && f.VestingMonths is > 0;
                decimal? vested = hasSchedule
                    ? f.EquityPercentage * vestingCalculator.VestedFraction(
                        f.VestingStartDate, f.VestingMonths, f.CliffMonths, asOf)
                    : null;

                yield return new TermSheetFounder(
                    f.Name,
                    f.EquityPercentage,
                    hasSchedule ? f.VestingStartDate : null,
                    hasSchedule ? f.VestingMonths : null,
                    hasSchedule ? f.CliffMonths ?? 0 : null,
                    vested);
            }
        }

        /// <summary>
        /// The scoring job falls back to an insufficient-data result with no methods when scoring
        /// fails. That case is decided here, once: the document says "no data" rather than presenting
        /// a fabricated 0/100 score and a ₽0 range as if they were real. Deciding it in each renderer
        /// instead is how the markdown and the PDF of one deal come to disagree.
        /// </summary>
        private static TermSheetScore ComposeScore(ScoreResult score)
        {
            if (score.MethodsUsed.Count == 0)
            {
                return TermSheetScore.Unavailable(score.CalculatedAt);
            }

            return new TermSheetScore(
                Available: true,
                Total: score.TotalScore,
                Team: score.TeamScore,
                Market: score.MarketScore,
                Product: score.ProductScore,
                Traction: score.TractionScore,
                Competition: score.CompetitionScore,
                ValuationLow: score.ValuationLow,
                ValuationHigh: score.ValuationHigh,
                MethodsUsed: score.MethodsUsed,
                MethodologyVersion: string.IsNullOrEmpty(score.MethodologyVersion) ? null : score.MethodologyVersion,
                CalculatedAt: score.CalculatedAt);
        }
    }
}
