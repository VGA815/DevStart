using DevStart.Domain.Startups;

namespace DevStart.Application.Scoring
{
    public sealed record ValuationRange(decimal Low, decimal High, IReadOnlyList<string> MethodsUsed);

    public interface IValuationCalculator
    {
        /// <summary>
        /// Computes a valuation range (low/high in RUB) based on the total score, stage and ARR.
        /// Uses an ensemble of methods (Berkus, Scorecard, VC Method, DCF, Comparable, First Chicago)
        /// with stage-dependent weights. When <paramref name="annualRecurringRevenue"/> &gt; 0 the
        /// Comparable method is anchored to real revenue (ARR × stage multiple); otherwise every method
        /// is score-scaled. Pass 0 for pre-revenue startups.
        /// </summary>
        ValuationRange ComputeRange(decimal totalScore, StartupStage stage, decimal annualRecurringRevenue);
    }
}
