namespace DevStart.Application.Abstractions.Authentication
{
    /// <summary>
    /// A short-lived record of a login that passed the first factor (password or OAuth) and is
    /// awaiting a TOTP code (<see cref="SetupRequired"/> = false) or mandatory 2FA enrollment
    /// (<see cref="SetupRequired"/> = true, admins only).
    /// </summary>
    public sealed record PendingTwoFactorLogin(
        Guid UserId,
        string? IpAddress,
        string? UserAgent,
        bool SetupRequired);

    public interface IPendingTwoFactorStore
    {
        Task SaveAsync(string token, PendingTwoFactorLogin entry, TimeSpan ttl, CancellationToken cancellationToken);

        /// <summary>Non-destructive read: the setup flow reads the token twice (setup, then confirm).</summary>
        Task<PendingTwoFactorLogin?> GetAsync(string token, CancellationToken cancellationToken);

        Task RemoveAsync(string token, CancellationToken cancellationToken);

        /// <summary>Atomically increments the failed-attempt counter and returns the new count.</summary>
        Task<long> IncrementAttemptsAsync(string token, CancellationToken cancellationToken);
    }
}
