using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupCommunityStandards;

namespace DevStart.Application.CommunityStandards.GetDocuments
{
    /// <summary>Metadata only — the Markdown body is served per document by <c>GetStartupCommunityDocumentQuery</c>.</summary>
    public sealed record CommunityDocumentSummary(
        Guid Id,
        CommunityDocumentType Type,
        string Title,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record GetStartupCommunityDocumentsQuery(Guid StartupId) : IQuery<List<CommunityDocumentSummary>>;
}
