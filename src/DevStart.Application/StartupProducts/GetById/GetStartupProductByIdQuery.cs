using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupProducts.GetById
{
    public sealed record GetStartupProductByIdQuery(Guid StartupId) : IQuery<StartupProductResponse>, ICacheableQuery
    {
        public string CacheKey => $"v1:startup-products:{StartupId}";

        public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
    }
}
