using DevStart.Domain.StartupCommunityStandards;

namespace DevStart.Application.CommunityStandards
{
    /// <summary>
    /// A single checklist row. Only the machine-readable shape is returned — human-facing titles,
    /// descriptions and "Add" call-to-actions are rendered by the client, the same way
    /// <c>ScoreResult</c> returns numbers and leaves the wording to the UI.
    /// </summary>
    /// <param name="Key">Stable identifier the client maps to a label, e.g. <c>description</c>.</param>
    /// <param name="IsDocument">
    /// True for the community documents, false for signals derived from the startup profile. Lets the
    /// client route "Add" either to the document editor or to the relevant profile screen.
    /// </param>
    /// <param name="DocumentType">Set for document rows; null for profile signals.</param>
    /// <param name="DocumentId">Set when the document exists, so the client can link straight to it.</param>
    public sealed record CommunityStandardsCheck(
        string Key,
        bool IsSatisfied,
        bool IsDocument,
        CommunityDocumentType? DocumentType,
        Guid? DocumentId);

    public sealed record CommunityStandardsResult(
        int CompletedCount,
        int TotalCount,
        decimal Percent,
        CommunityStandardsLevel Level,
        IReadOnlyList<CommunityStandardsCheck> Checks,
        DateTime EvaluatedAt);
}
