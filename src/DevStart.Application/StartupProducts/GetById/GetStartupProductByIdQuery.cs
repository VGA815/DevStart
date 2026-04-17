using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupProducts.GetById
{
    public sealed record GetStartupProductByIdQuery(Guid StartupId) : IQuery<StartupProductResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.StartupProduct(StartupId);

        public TimeSpan Expiration => CacheTtl.Default;
    }
}
