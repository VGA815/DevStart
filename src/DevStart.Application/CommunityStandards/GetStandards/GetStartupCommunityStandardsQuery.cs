using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.CommunityStandards.GetStandards
{
    /// <summary>Public entry point for a startup's community-standards checklist.</summary>
    public sealed record GetStartupCommunityStandardsQuery(Guid StartupId) : IQuery<CommunityStandardsResult>;
}
