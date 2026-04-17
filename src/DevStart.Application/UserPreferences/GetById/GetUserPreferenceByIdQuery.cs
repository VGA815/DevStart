using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.UserPreferences.GetById
{
    public sealed record GetUserPreferenceByIdQuery(Guid UserId) : IQuery<UserPreferenceResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.UserPreference(UserId);

        public TimeSpan Expiration => CacheTtl.Default;
    }
}
