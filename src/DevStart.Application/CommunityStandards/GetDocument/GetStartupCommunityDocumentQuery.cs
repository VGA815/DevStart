using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupCommunityStandards;

namespace DevStart.Application.CommunityStandards.GetDocument
{
    public sealed record CommunityDocumentResponse(
        Guid Id,
        Guid StartupId,
        CommunityDocumentType Type,
        string Title,
        string Content,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record GetStartupCommunityDocumentQuery(Guid StartupId, CommunityDocumentType Type)
        : IQuery<CommunityDocumentResponse>;
}
