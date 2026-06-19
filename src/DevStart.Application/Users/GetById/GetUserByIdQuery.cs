using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.GetById
{
    // Public, authorization-gated entry point. NOT cacheable: the own-account gate (UserId must equal
    // the caller) must run on every request. The actual user read is cached one layer down via
    // FetchUserByIdQuery (viewer-independent), so the gate can never be skipped on a cache hit —
    // preventing one user from reading another user's record (e.g. email) from a warm cache.
    public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserResponse>;
}
