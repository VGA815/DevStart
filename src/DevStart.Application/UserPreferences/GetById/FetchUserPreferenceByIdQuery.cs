using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.UserPreferences.GetById
{
    /// <summary>
    /// Internal, viewer-independent preference read. This is the cached unit of work and carries NO
    /// authorization gate. Must not be exposed via an endpoint — public access goes through
    /// <see cref="GetUserPreferenceByIdQuery"/>, which runs the own-account gate before delegating
    /// here, so a warm cache can never let one user read another user's preferences.
    /// </summary>
    internal sealed record FetchUserPreferenceByIdQuery(Guid UserId) : IQuery<UserPreferenceResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.UserPreference(UserId);

        public TimeSpan Expiration => CacheTtl.Default;
    }
}
