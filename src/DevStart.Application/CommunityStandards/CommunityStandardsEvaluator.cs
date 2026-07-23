using DevStart.Domain.StartupCommunityStandards;

namespace DevStart.Application.CommunityStandards
{
    /// <summary>
    /// Turns resolved signals into the twelve-item checklist. Pure and stateless (registered as a
    /// singleton), so the whole grading policy is testable without a database — same shape as
    /// <c>ScoringEngine</c>.
    /// </summary>
    internal sealed class CommunityStandardsEvaluator : ICommunityStandardsEvaluator
    {
        /// <summary>
        /// A startup of one is not a team. Two co-founders and no employees counts: that is a normal
        /// seed-stage shape, and requiring a non-founder would put the checklist out of reach of any
        /// startup that simply has not hired yet.
        /// </summary>
        private const int MinMembers = 2;

        /// <summary>Fewer than three items reads as a placeholder roadmap rather than a plan.</summary>
        private const int MinRoadmapItems = 3;

        /// <summary>Below this share of completed items the startup is not presenting itself at all.</summary>
        private const int DevelopingThreshold = 6;

        /// <summary>Every community document, in checklist order.</summary>
        private static readonly (CommunityDocumentType Type, string Key)[] DocumentChecks =
        [
            (CommunityDocumentType.CodeOfConduct,  "code_of_conduct"),
            (CommunityDocumentType.Contributing,   "contributing"),
            (CommunityDocumentType.Support,        "support"),
            (CommunityDocumentType.SecurityPolicy, "security_policy"),
            (CommunityDocumentType.Legal,          "legal")
        ];

        public CommunityStandardsResult Evaluate(CommunityStandardsInputs inputs, DateTime utcNow)
        {
            List<CommunityStandardsCheck> checks =
            [
                ProfileCheck("description", inputs.HasDescription),
                ProfileCheck("logo", inputs.HasLogo),
                ProfileCheck("links", inputs.HasLinks),
                ProfileCheck("product", inputs.HasArticulatedProduct),
                ProfileCheck("team", inputs.HasFounder && inputs.MemberCount >= MinMembers),
                ProfileCheck("pitch_deck", inputs.HasPitchDeck),
                ProfileCheck("roadmap", inputs.RoadmapItemCount >= MinRoadmapItems)
            ];

            foreach ((CommunityDocumentType type, string key) in DocumentChecks)
            {
                bool exists = inputs.Documents.TryGetValue(type, out Guid documentId);
                checks.Add(new CommunityStandardsCheck(key, exists, true, type, exists ? documentId : null));
            }

            int completed = checks.Count(c => c.IsSatisfied);
            int total = checks.Count;

            // Rounded to a whole percent: the client shows it as a badge, not a measurement.
            decimal percent = total == 0 ? 0m : Math.Round(completed * 100m / total, 0, MidpointRounding.AwayFromZero);

            return new CommunityStandardsResult(completed, total, percent, ResolveLevel(completed, total), checks, utcNow);
        }

        private static CommunityStandardsCheck ProfileCheck(string key, bool isSatisfied)
            => new(key, isSatisfied, false, null, null);

        // Complete is deliberately all-or-nothing, like GitHub's "checklist complete" — a startup that
        // is one document short is Developing, not Complete.
        private static CommunityStandardsLevel ResolveLevel(int completed, int total)
        {
            if (total > 0 && completed == total)
            {
                return CommunityStandardsLevel.Complete;
            }

            return completed >= DevelopingThreshold
                ? CommunityStandardsLevel.Developing
                : CommunityStandardsLevel.Incomplete;
        }
    }
}
