using DevStart.Application.Scoring;

namespace DevStart.Application.ScoringReports
{
    /// <summary>
    /// The scoring report as data. Same shape of contract as <c>TermSheetModel</c>: typed values, no
    /// formatting, and every judgement about what the document may claim already made.
    /// </summary>
    public sealed record ScoringReportModel(
        Guid StartupId,
        string StartupName,
        string StartupStage,
        bool Available,
        decimal? TotalScore,
        IReadOnlyList<ScoringReportFactor> Factors,
        decimal ValuationLow,
        decimal ValuationHigh,
        decimal ValuationPoint,
        IReadOnlyList<string> MethodsUsed,
        string? MethodologyVersion,
        DateTime CalculatedAt,
        DateTime GeneratedAt);

    /// <summary>
    /// One factor's line in the report.
    /// <para>
    /// <see cref="Score"/> is <c>null</c> for a factor that had no data and did not take part. That
    /// is not zero and must never be printed as zero: the factor's weight was redistributed across
    /// the remaining factors, so a zero would understate the result and there is no screen to ask a
    /// follow-up question on. <see cref="Participated"/> states it directly.
    /// </para>
    /// </summary>
    public sealed record ScoringReportFactor(
        string Factor,
        decimal? Score,
        decimal Weight,
        ScoreFactorSource Source)
    {
        public bool Participated => Score is not null;
    }
}
