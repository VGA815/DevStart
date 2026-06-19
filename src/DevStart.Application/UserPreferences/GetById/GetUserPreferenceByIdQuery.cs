using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.UserPreferences.GetById
{
    // Public, authorization-gated entry point. NOT cacheable: the own-account gate (UserId must equal
    // the caller) must run on every request. The actual preference read is cached one layer down via
    // FetchUserPreferenceByIdQuery (viewer-independent), so the gate can never be skipped on a cache
    // hit — preventing one user from reading another user's preferences from a warm cache.
    public sealed record GetUserPreferenceByIdQuery(Guid UserId) : IQuery<UserPreferenceResponse>;
}
