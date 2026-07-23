using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.CommunityStandards.GetTemplates
{
    public sealed record GetCommunityDocumentTemplatesQuery : IQuery<List<CommunityDocumentTemplate>>;
}
