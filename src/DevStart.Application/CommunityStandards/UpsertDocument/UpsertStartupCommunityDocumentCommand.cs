using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupCommunityStandards;

namespace DevStart.Application.CommunityStandards.UpsertDocument
{
    /// <summary>
    /// Creates or replaces the startup's document of this type. A startup has at most one document per
    /// type, so the endpoint is a PUT and there is no separate create/update split.
    /// </summary>
    public sealed record UpsertStartupCommunityDocumentCommand(
        Guid StartupId,
        CommunityDocumentType Type,
        string Title,
        string Content) : ICommand<Guid>;
}
