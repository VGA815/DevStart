using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Startups.GetById
{
    public sealed record GetStartupByIdQuery(Guid StartupId) : IQuery<StartupResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.Startup(StartupId);

        public TimeSpan Expiration => CacheTtl.Default;
    }
}
