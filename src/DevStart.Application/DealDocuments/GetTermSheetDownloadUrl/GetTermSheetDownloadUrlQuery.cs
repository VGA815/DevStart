using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.DealDocuments.GetTermSheetDownloadUrl
{
    /// <summary>Which rendering of the term sheet the download link should point at.</summary>
    public enum TermSheetFormat
    {
        /// <summary>The markdown source, for a reader who wants to edit the text.</summary>
        Markdown = 0,

        /// <summary>The paginated PDF — the form meant to be read, printed and passed on.</summary>
        Pdf = 1,
    }

    /// <summary>
    /// Both formats travel through this one query, and therefore through one access check: the deal
    /// must exist, the caller is the investor or a founder/administrator of the startup, and an
    /// investor additionally needs active Pro or a paid term-sheet entitlement for this deal. Adding
    /// a separate endpoint for the PDF would have meant a second copy of that rule to keep in step.
    /// </summary>
    public sealed record GetTermSheetDownloadUrlQuery(
        Guid DealId,
        TermSheetFormat Format = TermSheetFormat.Markdown) : IQuery<TermSheetDownloadUrlResponse>;

    public sealed class TermSheetDownloadUrlResponse
    {
        public Guid DealId { get; init; }
        public string Url { get; init; } = null!;
        public DateTime ExpiresAt { get; init; }
        public TermSheetFormat Format { get; init; }

        /// <summary>The name the file is saved under, already applied to the presigned link.</summary>
        public string FileName { get; init; } = null!;

        /// <summary>
        /// SHA-256 of the PDF, lower-case hex; <c>null</c> for markdown, which is not hashed. Lets a
        /// holder of the file check that it is the document the platform generated.
        /// </summary>
        public string? Sha256 { get; init; }
    }
}
