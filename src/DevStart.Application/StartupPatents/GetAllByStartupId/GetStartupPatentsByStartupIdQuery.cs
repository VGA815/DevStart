using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupPatents.GetAllByStartupId
{
    /// <summary>
    /// The IP block of the Product tab. Cacheable and ungated: it carries the same thing for every
    /// reader — no Pro-only or member-only field is folded in — so the cache decorator sitting outside
    /// the handler cannot serve one audience's view to another.
    /// </summary>
    public sealed class GetStartupPatentsByStartupIdQuery
        : IQuery<StartupPatentsResponse>, ICacheableQuery
    {
        public Guid StartupId { get; set; }

        public GetStartupPatentsByStartupIdQuery(Guid startupId)
        {
            StartupId = startupId;
        }

        public string CacheKey => CacheKeys.StartupPatents(StartupId);

        public TimeSpan Expiration => CacheTtl.Default;
    }
}
