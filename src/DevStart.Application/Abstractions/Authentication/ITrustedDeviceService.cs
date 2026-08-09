using DevStart.Domain.Security;
using DevStart.Domain.Users;

namespace DevStart.Application.Abstractions.Authentication
{
    /// <summary>A freshly minted device token. The raw value is returned exactly once.</summary>
    public sealed record IssuedTrustedDevice(string RawToken, Guid DeviceId, DateTime ExpiresAt);

    public interface ITrustedDeviceService
    {
        /// <summary>
        /// True when <paramref name="rawToken"/> identifies an active device of this user under the
        /// given policy, in which case the row's last-used stamp and IP are updated. The token
        /// itself is <em>not</em> rotated — see <see cref="Domain.TrustedDevices.TrustedDevice"/> for
        /// why. Returns false for every other reason — expired, revoked, unknown, wrong user, wrong
        /// network, unrecognized policy, feature off — so the caller cannot turn the outcome into an
        /// oracle.
        /// </summary>
        Task<bool> TryConsumeAsync(
            User user,
            string? rawToken,
            string? ipAddress,
            TwoFactorStrictness strictness,
            CancellationToken cancellationToken);

        /// <summary>
        /// Trusts the browser that just proved a second factor. Returns null when the feature is off
        /// or the user's policy is <see cref="TwoFactorStrictness.EveryLogin"/>.
        /// </summary>
        Task<IssuedTrustedDevice?> IssueAsync(
            User user,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken);

        Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
    }
}
