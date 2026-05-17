using DevStart.Domain.Users;
using DevStart.SharedKernel;

namespace DevStart.Application.Abstractions.Authentication
{
    public sealed record IssuedRefreshToken(string RawToken, DateTime ExpiresAt);

    public sealed record RotatedTokens(string RawRefreshToken, DateTime RefreshExpiresAt, Guid UserId);

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

        Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
    }
}
