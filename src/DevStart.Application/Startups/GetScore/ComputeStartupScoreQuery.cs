using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Scoring;

namespace DevStart.Application.Startups.GetScore
{
    /// <summary>
    /// Internal, viewer-independent score computation. This is the cached unit of work
    /// (heavy DB reads + scoring + valuation) and carries NO authorization gate.
    /// Must not be exposed via an endpoint — public access goes through
    /// <see cref="GetStartupScoreQuery"/>, which runs the Pro/member gate before delegating here.
    /// Background jobs (e.g. term-sheet generation) call this directly since they have no user context.
    /// </summary>
    public sealed record ComputeStartupScoreQuery(Guid StartupId) : IQuery<ScoreResult>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.StartupScore(StartupId);

        public TimeSpan Expiration => TimeSpan.FromHours(1);
    }
}
