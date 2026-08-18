namespace DevStart.Application.ScoringReports
{
    /// <summary>
    /// Renders the scoring report to a PDF. Deterministic, like the term-sheet renderer: the same
    /// model always yields the same bytes, so one computation of a startup's score corresponds to one
    /// stored file.
    /// </summary>
    public interface IScoringReportPdfRenderer
    {
        byte[] Render(ScoringReportModel model);
    }

    public static class ScoringReportStorage
    {
        public const string Bucket = "scoring-reports";

        /// <summary>
        /// Keyed by the moment the score was computed, not by "latest". A report is a statement about
        /// a startup at one point in time; overwriting one key would silently replace a document
        /// somebody has already been handed with a different set of numbers under the same name.
        /// <para>
        /// The key ends in <c>Z</c>, so the value it is built from has to actually be UTC. Values
        /// reaching here are UTC in practice — they come from <c>IDateTimeProvider.UtcNow</c> or from
        /// a <c>timestamptz</c> column — but a <see cref="DateTimeKind.Local"/> value slipping in would
        /// otherwise produce a key that means a different instant on a machine in another timezone,
        /// and the same score would be stored twice under two names. Normalized rather than trusted.
        /// </para>
        /// </summary>
        public static string ObjectKey(Guid startupId, DateTime calculatedAt)
        {
            DateTime utc = calculatedAt.Kind switch
            {
                DateTimeKind.Utc => calculatedAt,
                DateTimeKind.Local => calculatedAt.ToUniversalTime(),
                // Unspecified is the platform's storage convention for "already UTC": relabel it
                // rather than convert, because ToUniversalTime would treat it as local and shift it.
                _ => DateTime.SpecifyKind(calculatedAt, DateTimeKind.Utc),
            };

            return $"scoring-reports/{startupId}/{utc:yyyyMMdd'T'HHmmss}Z.pdf";
        }
    }
}
