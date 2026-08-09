using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Auth.Sessions.GetSessions
{
    /// <summary>
    /// The caller's own active sessions. Deliberately not <see cref="ICacheableQuery"/>: the answer is
    /// per-user, changes on every refresh, and is the screen a worried user stares at.
    /// </summary>
    public sealed record GetSessionsQuery : IQuery<IReadOnlyList<SessionResponse>>;

    public sealed record SessionResponse(
        Guid Id,
        bool Current,
        DateTime CreatedAt,
        DateTime LastUsedAt,
        DateTime ExpiresAt,
        string? IpAddress,
        string Browser,
        string Os,
        string DeviceKind);
}
