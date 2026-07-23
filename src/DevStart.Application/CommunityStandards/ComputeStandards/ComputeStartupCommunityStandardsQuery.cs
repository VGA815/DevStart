using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.CommunityStandards.ComputeStandards
{
    /// <summary>
    /// The cached evaluation. Deliberately carries no visibility rules of its own — every gate lives in
    /// <c>GetStartupCommunityStandardsQuery</c>, which runs on each request, so a warm cache can never
    /// keep serving a startup that was banned in the meantime.
    /// </summary>
    public sealed record ComputeStartupCommunityStandardsQuery(Guid StartupId)
        : IQuery<CommunityStandardsResult>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.StartupCommunityStandards(StartupId);

        public TimeSpan Expiration => CacheTtl.Default;
    }
}
