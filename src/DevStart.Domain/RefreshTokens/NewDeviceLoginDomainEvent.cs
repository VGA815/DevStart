using DevStart.SharedKernel;

namespace DevStart.Domain.RefreshTokens
{
    /// <summary>
    /// A session was opened from a browser/OS combination this user has not signed in from recently.
    /// Carries the email so the handler never has to load the user again.
    /// </summary>
    public sealed record NewDeviceLoginDomainEvent(
        Guid UserId,
        string Email,
        string? Browser,
        string? Os,
        string? IpAddress,
        DateTime OccurredAtUtc) : IDomainEvent;
}
