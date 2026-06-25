using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Startups.RecomputeValuation
{
    /// <summary>
    /// Recomputes a startup's score + valuation and persists a <c>StartupValuationSnapshot</c>.
    /// The explicit on-demand / backfill entry point for the snapshot history (the live read path keeps
    /// recomputing + caching). Returns the new snapshot id. Event/schedule-driven triggering is deferred.
    /// </summary>
    public sealed record RecomputeStartupValuationCommand(Guid StartupId) : ICommand<Guid>;
}
