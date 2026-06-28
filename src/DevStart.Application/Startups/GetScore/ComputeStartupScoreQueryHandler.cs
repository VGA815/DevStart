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
        IValuationBenchmarkProvider benchmarkProvider,
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

            // The valuation ensemble reads the engine sub-scores plus the raw signals (stage, industry,
            // ARR, target round amount, partnerships) the provider already resolved, and the pre-money
            // medians / revenue multiples as of the valuation date.
            ValuationBenchmarkSet benchmarks = await benchmarkProvider.GetAsync(utcNow, cancellationToken);
            ValuationResult valuation = valuationCalculator.Compute(baseScore, inputs, benchmarks);

            return baseScore with
            {
                ValuationLow = valuation.Low,
                ValuationHigh = valuation.High,
                ValuationPoint = valuation.Point,
                MethodsUsed = valuation.MethodsUsed,
                ValuationMethods = valuation.Methods,
                MethodologyVersion = valuation.MethodologyVersion
            };
        }
    }
}
