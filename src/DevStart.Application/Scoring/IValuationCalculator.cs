namespace DevStart.Application.Scoring
{
    /// <summary>One method's contribution to the valuation ensemble.</summary>
    /// <param name="Method">Method name (e.g. "Berkus").</param>
    /// <param name="Value">Point estimate from the method (RUB).</param>
    /// <param name="Weight">Renormalized weight of this method within the applicable ensemble (0..1).</param>
    /// <param name="Assumptions">Human-readable notes on the assumptions/proxies applied.</param>
    public sealed record ValuationBreakdown(
        string Method,
        decimal Value,
        decimal Weight,
        IReadOnlyList<string> Assumptions);

    /// <summary>
    /// Result of the valuation ensemble. <see cref="Low"/> ≤ <see cref="Point"/> ≤ <see cref="High"/>.
    /// When no method applies to the stage, <see cref="MethodsUsed"/> is empty and the range is 0 — an
    /// explicit "insufficient data" signal (consumers render N/A rather than a fabricated ₽0).
    /// </summary>
    public sealed record ValuationResult(
        decimal Low,
        decimal High,
        decimal Point,
        IReadOnlyList<string> MethodsUsed,
        IReadOnlyList<ValuationBreakdown> Methods,
        string MethodologyVersion)
    {
        public static ValuationResult InsufficientData(string methodologyVersion) =>
            new(0m, 0m, 0m, [], [], methodologyVersion);
    }

    public interface IValuationCalculator
    {
        /// <summary>
        /// Computes a valuation range (RUB) from an ensemble of stage-applicable methods
        /// (Berkus, Scorecard, VC Method). Sub-scores are read from <paramref name="score"/>; stage,
        /// industry, ARR, target round amount and the partnerships signal from <paramref name="inputs"/>.
        /// Weights are renormalized to sum 1.0 over the methods that apply to the stage. All constants
        /// are read from <see cref="ValuationOptions"/>.
        /// </summary>
        ValuationResult Compute(ScoreResult score, ScoringInputs inputs);
    }
}
