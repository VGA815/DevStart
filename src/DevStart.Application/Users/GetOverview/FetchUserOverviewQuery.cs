using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.GetOverview
{
    /// <summary>
    /// Internal, viewer-independent user overview read. This is the cached unit of work and carries
    /// NO authorization gate or redaction — it returns the full aggregate (including the owner-only
    /// Email and TotalInvestedAmount). Must not be exposed via an endpoint: public access goes
    /// through <see cref="GetUserOverviewQuery"/>, which resolves the viewer and redacts private
    /// fields for non-owners AFTER this cached read, so a warm cache can never leak another user's
    /// private fields.
    /// </summary>
    internal sealed record FetchUserOverviewQuery(Guid UserId) : IQuery<UserOverviewResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.UserOverview(UserId);

        public TimeSpan Expiration => CacheTtl.Default;
    }
}
