using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Scoring;

namespace DevStart.Application.Startups.GetScore
{
    public sealed record GetStartupScoreQuery(Guid StartupId) : IQuery<ScoreResult>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.StartupScore(StartupId);

        public TimeSpan Expiration => TimeSpan.FromHours(1);
    }
}
