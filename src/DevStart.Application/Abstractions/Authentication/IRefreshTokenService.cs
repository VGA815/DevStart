using DevStart.Domain.Users;
using DevStart.SharedKernel;

namespace DevStart.Application.Abstractions.Authentication
{
    public sealed record IssuedRefreshToken(string RawToken, DateTime ExpiresAt, Guid SessionId);

    public sealed record RotatedTokens(string RawRefreshToken, DateTime RefreshExpiresAt, Guid UserId, Guid SessionId);

    public interface IRefreshTokenService
    {
        Task<IssuedRefreshToken> IssueAsync(
            User user,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken);

        Task<Result<RotatedTokens>> RotateAsync(
            string rawRefreshToken,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken);

        Task<Result> RevokeAsync(string rawRefreshToken, CancellationToken cancellationToken);

        /// <summary>
        /// Revokes every active refresh token <em>and every trusted device</em> for the user — i.e.
        /// full re-authentication with a second factor, everywhere. The name under-describes it on
        /// purpose: every caller (password change/reset, 2FA enable/disable/reset, ban, refresh-token
        /// reuse) is a credential-invalidation event where leaving devices trusted would be a hole,
        /// so the two are deliberately impossible to invoke separately.
        /// </summary>
        Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
    }
}
