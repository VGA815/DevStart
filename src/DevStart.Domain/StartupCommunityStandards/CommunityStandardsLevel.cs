namespace DevStart.Domain.StartupCommunityStandards
{
    /// <summary>
    /// Coarse grade of a startup's community-standards checklist, used for the catalog badge and the
    /// catalog filter. The thresholds live in the evaluator, not here.
    /// </summary>
    public enum CommunityStandardsLevel
    {
        Incomplete = 0,
        Developing = 1,
        Complete   = 2
    }
}
