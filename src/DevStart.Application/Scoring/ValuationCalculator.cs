using DevStart.Domain.Startups;

namespace DevStart.Application.Scoring
{
    /// <summary>
    /// Ensemble of stage-specific valuation methods. All values returned in RUB.
    /// Score-scaled methods (Berkus, Scorecard, Expert, VC, DCF, First Chicago) are linear
    /// approximations chosen for MVP — not real DCF/Comparable analytics. The Comparable method is
    /// revenue-anchored when ARR is available (ARR × stage multiple), which makes the blended range
    /// track actual revenue instead of being a pure function of the score. The calculator blends the
    /// per-stage methods and emits a ±25% range as a low/high.
    /// </summary>
    internal sealed class ValuationCalculator : IValuationCalculator
    {
        private const decimal RangeLowFactor = 0.75m;
        private const decimal RangeHighFactor = 1.25m;

        // Revenue multiples for the Comparable method. Proposed defaults (tunable, RU market).
        private const decimal SeedRevenueMultiple = 8m;
        private const decimal SeriesARevenueMultiple = 6m;

        public ValuationRange ComputeRange(decimal totalScore, StartupStage stage, decimal annualRecurringRevenue)
        {
            decimal scoreFactor = totalScore / 100m;
            decimal arr = Math.Max(0m, annualRecurringRevenue);

            (decimal blended, IReadOnlyList<string> methods) = stage switch
            {
                StartupStage.Idea or StartupStage.PreSeed => BlendEarly(scoreFactor),
                StartupStage.Mvp or StartupStage.Seed => BlendSeed(scoreFactor, arr),
                StartupStage.SeriesA => BlendSeriesA(scoreFactor, arr),
                _ => BlendEarly(scoreFactor)
            };

            decimal low = Math.Round(blended * RangeLowFactor, 0, MidpointRounding.AwayFromZero);
            decimal high = Math.Round(blended * RangeHighFactor, 0, MidpointRounding.AwayFromZero);

            return new ValuationRange(low, high, methods);
        }

        // Pre-seed / Idea: Berkus 0.4 + Scorecard 0.4 + Expert 0.2 (qualitative, pre-revenue — no Comparable)
        private static (decimal blended, IReadOnlyList<string> methods) BlendEarly(decimal sf)
        {
            decimal berkus = Berkus(sf);
            decimal scorecard = Scorecard(sf);
            decimal expert = Expert(sf);

            decimal blended = berkus * 0.4m + scorecard * 0.4m + expert * 0.2m;
            return (blended, new[] { "Berkus", "Scorecard", "Expert" });
        }

        // Mvp / Seed: Scorecard 0.3 + VC Method 0.3 + Comparable 0.3 + DCF 0.1
        private static (decimal blended, IReadOnlyList<string> methods) BlendSeed(decimal sf, decimal arr)
        {
            decimal scorecard = Scorecard(sf);
            decimal vc = VcMethod(sf);
            decimal comparable = Comparable(sf, arr, SeedRevenueMultiple);
            decimal dcf = Dcf(sf);

            decimal blended = scorecard * 0.3m + vc * 0.3m + comparable * 0.3m + dcf * 0.1m;
            return (blended, new[] { "Scorecard", "VcMethod", "Comparable", "Dcf" });
        }

        // Series A: VC Method 0.25 + DCF 0.25 + Comparable 0.30 + First Chicago 0.20
        private static (decimal blended, IReadOnlyList<string> methods) BlendSeriesA(decimal sf, decimal arr)
        {
            decimal vc = VcMethod(sf);
            decimal dcf = Dcf(sf);
            decimal comparable = Comparable(sf, arr, SeriesARevenueMultiple);
            decimal firstChicago = FirstChicago(sf);

            decimal blended = vc * 0.25m + dcf * 0.25m + comparable * 0.30m + firstChicago * 0.20m;
            return (blended, new[] { "VcMethod", "Dcf", "Comparable", "FirstChicago" });
        }

        // Berkus: 5 elements × ₽45M max each, scaled by score → max ₽225M
        private static decimal Berkus(decimal sf) => 225_000_000m * sf;

        // Scorecard: median pre-seed valuation in RUB scaled by score, with floor 50%
        private static decimal Scorecard(decimal sf) => 120_000_000m * (0.5m + sf);

        // Expert opinion: linear ₽30M–₽250M based on score
        private static decimal Expert(decimal sf) => 30_000_000m + (220_000_000m * sf);

        // VC Method: assume 10× return in 5 years from a target ₽50M slice
        // → today_val = 50M / 10 × (1 + score) ≈ 5M..10M, scaled up to startup-equivalent
        private static decimal VcMethod(decimal sf) => 200_000_000m * (0.5m + sf);

        // Comparable: revenue multiple on actual ARR when available; score-scaled proxy when pre-revenue.
        private static decimal Comparable(decimal sf, decimal arr, decimal revenueMultiple) =>
            arr > 0m
                ? arr * revenueMultiple
                : 250_000_000m * (0.4m + sf);

        // DCF (simplified): linear ₽100M–₽600M from score
        private static decimal Dcf(decimal sf) => 100_000_000m + (500_000_000m * sf);

        // First Chicago: weighted scenarios (best 30% / base 50% / worst 20%)
        private static decimal FirstChicago(decimal sf)
        {
            decimal best = 800_000_000m * sf;
            decimal baseCase = 350_000_000m * (0.5m + sf);
            decimal worst = 80_000_000m;
            return best * 0.30m + baseCase * 0.50m + worst * 0.20m;
        }
    }
}
