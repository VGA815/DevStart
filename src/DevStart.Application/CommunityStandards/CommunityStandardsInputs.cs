using DevStart.Domain.StartupCommunityStandards;

namespace DevStart.Application.CommunityStandards
{
    /// <summary>
    /// Raw signals the checklist is evaluated from. Resolved by <see cref="ICommunityStandardsDataProvider"/>
    /// so <see cref="ICommunityStandardsEvaluator"/> stays a pure function over data — the same split as
    /// <c>ScoringInputs</c> / <c>IScoringEngine</c>.
    /// </summary>
    public sealed record CommunityStandardsInputs(
        Guid StartupId,
        bool HasDescription,
        bool HasLogo,
        bool HasLinks,
        bool HasArticulatedProduct,
        int MemberCount,
        bool HasFounder,
        bool HasPitchDeck,
        int RoadmapItemCount,
        IReadOnlyDictionary<CommunityDocumentType, Guid> Documents);
}
