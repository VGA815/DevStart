using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupCommunityStandards;

namespace DevStart.Application.CommunityStandards.DeleteDocument
{
    /// <summary>Removing the document is how a startup un-publishes it — there is no draft state.</summary>
    public sealed record DeleteStartupCommunityDocumentCommand(Guid StartupId, CommunityDocumentType Type) : ICommand;
}
