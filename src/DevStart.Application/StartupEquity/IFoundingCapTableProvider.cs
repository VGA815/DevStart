namespace DevStart.Application.StartupEquity
{
    /// <summary>
    /// Resolves a startup's effective founding cap table: the persisted holders if the founders
    /// have set one, otherwise a bootstrapped default (founders split the non-ESOP pool equally
    /// plus a default ESOP row). The default is never written to the database, so it is safe to
    /// call from read paths and background jobs alike.
    /// </summary>
    public interface IFoundingCapTableProvider
    {
        Task<IReadOnlyList<FoundingCapTableHolder>> GetEffectiveHoldersAsync(
            Guid startupId,
            CancellationToken cancellationToken);
    }
}
