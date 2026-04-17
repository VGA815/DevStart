using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.GetById
{
    public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.User(UserId);

        public TimeSpan Expiration => CacheTtl.Default;
    }
}
