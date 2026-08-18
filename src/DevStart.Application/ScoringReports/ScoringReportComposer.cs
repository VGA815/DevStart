using DevStart.Application.Scoring;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;

namespace DevStart.Application.ScoringReports
{
    public interface IScoringReportComposer
    {
        ScoringReportModel Compose(Startup startup, ScoreResult score);
    }

    internal sealed class ScoringReportComposer(IDateTimeProvider dateTimeProvider) : IScoringReportComposer
    {
        public ScoringReportModel Compose(Startup startup, ScoreResult score)
        {
            // Same rule as the term sheet, and for the same reason: with no methods behind it there is
            // no result to report, and printing 0/100 with a ₽0–₽0 range would be a fabrication that
            // the reader carries away as if it were a finding.
            bool available = score.MethodsUsed.Count > 0;

            return new ScoringReportModel(
                StartupId: startup.Id,
                StartupName: startup.Name,
                StartupStage: startup.Stage.ToString(),
                Available: available,
                TotalScore: available ? score.TotalScore : null,
                Factors: available ? [.. score.Factors.Select(ToFactor)] : [],
                ValuationLow: available ? score.ValuationLow : 0m,
                ValuationHigh: available ? score.ValuationHigh : 0m,
                ValuationPoint: available ? score.ValuationPoint : 0m,
                MethodsUsed: available ? score.MethodsUsed : [],
                MethodologyVersion: available && !string.IsNullOrEmpty(score.MethodologyVersion)
                    ? score.MethodologyVersion
                    : null,
                CalculatedAt: score.CalculatedAt,
                GeneratedAt: dateTimeProvider.UtcNow);
        }

        private static ScoringReportFactor ToFactor(ScoreFactorBreakdown breakdown) =>
            new(breakdown.Factor, breakdown.Score, breakdown.Weight, breakdown.Source);
    }
}
