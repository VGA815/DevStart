using DevStart.SharedKernel;

namespace DevStart.Application.Scoring
{
    /// <summary>
    /// Assembles the <see cref="ScoringInputs"/> for a startup from the domain data. This is the single
    /// boundary between persistence and the pure <see cref="IScoringEngine"/> / <see cref="IValuationCalculator"/>:
    /// the engine never touches the database, and the "what counts as a scoring input" rules
    /// (latest-metric-per-type, metric fallbacks, derived signals) live here.
    /// </summary>
    public interface IScoringDataProvider
    {
        Task<Result<ScoringInputs>> GetInputsAsync(Guid startupId, CancellationToken cancellationToken);
    }
}
