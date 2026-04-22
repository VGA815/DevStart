using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupRoadmapItems.GetById
{
    public sealed record GetStartupRoadmapItemByIdQuery(Guid ItemId) : IQuery<StartupRoadmapItemResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.StartupRoadmapItem(ItemId);
        public TimeSpan Expiration => CacheTtl.Default;
    }
}
