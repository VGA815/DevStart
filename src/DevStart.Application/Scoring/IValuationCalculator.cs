using DevStart.Domain.Startups;

namespace DevStart.Application.Scoring
{
    public sealed record ValuationRange(decimal Low, decimal High, IReadOnlyList<string> MethodsUsed);

    public interface IValuationCalculator
    {
        /// <summary>
        /// Computes a valuation range (low/high in RUB) based on the total score and stage.
        /// Uses an ensemble of methods (Berkus, Scorecard, VC Method, DCF, Comparable, First Chicago)
        /// with stage-dependent weights, as defined in the spec.
        /// </summary>
        ValuationRange ComputeRange(decimal totalScore, StartupStage stage);
    }
}
