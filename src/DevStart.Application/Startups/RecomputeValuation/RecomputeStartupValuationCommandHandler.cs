using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Scoring;
using DevStart.Application.Startups.GetScore;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using System.Text.Json;

namespace DevStart.Application.Startups.RecomputeValuation
{
    internal sealed class RecomputeStartupValuationCommandHandler(
        IQueryHandler<ComputeStartupScoreQuery, ScoreResult> scoreHandler,
        IApplicationDbContext context)
        : ICommandHandler<RecomputeStartupValuationCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(RecomputeStartupValuationCommand command, CancellationToken cancellationToken)
        {
            Result<ScoreResult> scoreResult = await scoreHandler.Handle(
                new ComputeStartupScoreQuery(command.StartupId), cancellationToken);

            if (scoreResult.IsFailure)
            {
                return Result.Failure<Guid>(scoreResult.Error);
            }

            ScoreResult score = scoreResult.Value;
            if (score.MethodsUsed.Count == 0)
            {
                // No method applied to the stage — don't store a fabricated 0/0 snapshot.
                return Result.Failure<Guid>(ValuationErrors.InsufficientData);
            }

            string? breakdownJson = score.ValuationMethods is { Count: > 0 }
                ? JsonSerializer.Serialize(score.ValuationMethods, ValuationSnapshotJson.Options)
                : null;

            StartupValuationSnapshot snapshot = StartupValuationSnapshot.Create(
                command.StartupId,
                score.TotalScore, score.TeamScore, score.MarketScore, score.ProductScore,
                score.TractionScore, score.CompetitionScore,
                score.ValuationLow, score.ValuationHigh, score.ValuationPoint,
                string.Join(",", score.MethodsUsed),
                breakdownJson,
                score.MethodologyVersion,
                score.CalculatedAt);

            context.StartupValuationSnapshots.Add(snapshot);
            await context.SaveChangesAsync(cancellationToken);

            return snapshot.Id;
        }
    }
}
