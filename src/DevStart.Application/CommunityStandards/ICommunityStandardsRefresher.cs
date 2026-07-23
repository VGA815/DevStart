namespace DevStart.Application.CommunityStandards
{
    /// <summary>
    /// Recomputes a startup's checklist, upserts the catalog projection row and drops the cached read.
    /// Called from every write that can change a checklist outcome (community documents, startup profile
    /// updates) and from the nightly sweep that catches the rest.
    /// </summary>
    public interface ICommunityStandardsRefresher
    {
        Task RefreshAsync(Guid startupId, CancellationToken cancellationToken);
    }
}
