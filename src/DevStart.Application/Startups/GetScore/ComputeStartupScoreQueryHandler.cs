using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Scoring;
using DevStart.SharedKernel;

namespace DevStart.Application.Startups.GetScore
{
    internal sealed class ComputeStartupScoreQueryHandler(
        IScoringDataProvider dataProvider,
        IScoringEngine scoringEngine,
        IValuationCalculator valuationCalculator,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<ComputeStartupScoreQuery, ScoreResult>
    {
        public async Task<Result<ScoreResult>> Handle(ComputeStartupScoreQuery query, CancellationToken cancellationToken)
        {
            Result<ScoringInputs> inputsResult = await dataProvider.GetInputsAsync(query.StartupId, cancellationToken);
            if (inputsResult.IsFailure)
            {
                return Result.Failure<ScoreResult>(inputsResult.Error);
            }

            ScoringInputs inputs = inputsResult.Value;

            DateTime utcNow = dateTimeProvider.UtcNow;
            ScoreResult baseScore = scoringEngine.Compute(inputs, utcNow);

            // ARR anchors the revenue-multiple comparable in the valuation ensemble. The provider already
            // resolved and floored the traction signals, so ARR derives from the same MRR the engine scored.
            ValuationRange range = valuationCalculator.ComputeRange(
                baseScore.TotalScore, inputs.Stage, inputs.Traction.AnnualRecurringRevenue);

            return baseScore with
            {
                ValuationLow = range.Low,
                ValuationHigh = range.High,
                MethodsUsed = range.MethodsUsed
            };
        }
    }
}
