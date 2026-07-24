namespace DevStart.Application.Scoring
{
    public interface IScoringEngine
    {
        /// <summary>
        /// Computes the breakdown and total score for a startup based on its inputs. Factors with no
        /// data drop out and the remaining weights are renormalized to sum 1.0; when no factor has data
        /// the result is an explicit insufficient-data signal (<c>TotalScore = null</c>).
        /// <paramref name="benchmarks"/> supplies the external half of the competition factor (sector
        /// intensity) — the same as-of set the valuation reads.
        /// Returns ScoreResult with ValuationLow/High = 0 and MethodsUsed = []. Combine with
        /// IValuationCalculator to fill in the valuation range.
        /// </summary>
        ScoreResult Compute(ScoringInputs inputs, ValuationBenchmarkSet benchmarks, DateTime calculatedAt);
    }
}
