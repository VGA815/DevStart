using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Profiles.GetById
{
    public sealed record GetProfileByIdQuery(Guid UserId) : IQuery<ProfileResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.Profile(UserId);

        public TimeSpan Expiration => CacheTtl.Default;
    }
}
