using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ScoringReports.GetScoringReportDownloadUrl
{
    public sealed record GetScoringReportDownloadUrlQuery(Guid StartupId)
        : IQuery<ScoringReportDownloadUrlResponse>;

    public sealed class ScoringReportDownloadUrlResponse
    {
        public Guid StartupId { get; init; }
        public string Url { get; init; } = null!;
        public DateTime ExpiresAt { get; init; }
        public string FileName { get; init; } = null!;

        /// <summary>SHA-256 of the PDF, lower-case hex.</summary>
        public string Sha256 { get; init; } = null!;

        /// <summary>When the score behind this report was computed — what the file is a statement about.</summary>
        public DateTime CalculatedAt { get; init; }
    }
}
