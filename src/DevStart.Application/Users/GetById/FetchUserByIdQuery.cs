using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.GetById
{
    /// <summary>
    /// Internal, viewer-independent user read. This is the cached unit of work and carries NO
    /// authorization gate. Must not be exposed via an endpoint — public access goes through
    /// <see cref="GetUserByIdQuery"/>, which runs the own-account gate before delegating here,
    /// so a warm cache can never let one user read another user's record.
    /// </summary>
    internal sealed record FetchUserByIdQuery(Guid UserId) : IQuery<UserResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.User(UserId);

        public TimeSpan Expiration => CacheTtl.Default;
    }
}
