namespace DevStart.Application.Scoring
{
    public interface IScoringEngine
    {
        /// <summary>
        /// Computes the breakdown and total score for a startup based on its inputs.
        /// Returns ScoreResult with ValuationLow/High = 0 and MethodsUsed = []. Combine with IValuationCalculator
        /// to fill in the valuation range.
        /// </summary>
        ScoreResult Compute(ScoringInputs inputs, DateTime calculatedAt);
    }
}
