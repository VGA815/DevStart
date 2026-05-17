using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.RefreshTokens;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace DevStart.Infrastructure.Authentication.RefreshTokens
{
    internal sealed class RefreshTokenService(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        IOptions<RefreshTokenOptions> options)
        : IRefreshTokenService
    {
        private readonly TimeSpan _lifetime = TimeSpan.FromDays(options.Value.LifetimeDays);

        public async Task<IssuedRefreshToken> IssueAsync(
            User user,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            string raw = GenerateRawToken();
            string hash = RefreshTokenHasher.Hash(raw);
            DateTime now = dateTimeProvider.UtcNow;

            RefreshToken token = RefreshToken.Create(user.Id, hash, now, _lifetime, ipAddress, userAgent);
            context.RefreshTokens.Add(token);

            await context.SaveChangesAsync(cancellationToken);

            return new IssuedRefreshToken(raw, token.ExpiresAt);
        }

        public async Task<Result<RotatedTokens>> RotateAsync(
            string rawRefreshToken,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                return Result.Failure<RotatedTokens>(RefreshTokenErrors.Invalid);
            }

            string hash = RefreshTokenHasher.Hash(rawRefreshToken);
            DateTime now = dateTimeProvider.UtcNow;

            RefreshToken? existing = await context.RefreshTokens
                .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

            if (existing is null)
            {
                return Result.Failure<RotatedTokens>(RefreshTokenErrors.Invalid);
            }

            if (existing.IsRevoked)
            {
                await RevokeAllForUserAsync(existing.UserId, cancellationToken);
                return Result.Failure<RotatedTokens>(RefreshTokenErrors.ReuseDetected);
            }

            if (existing.IsExpired(now))
            {
                return Result.Failure<RotatedTokens>(RefreshTokenErrors.Expired);
            }

            string newRaw = GenerateRawToken();
            string newHash = RefreshTokenHasher.Hash(newRaw);

            RefreshToken replacement = RefreshToken.Create(
                existing.UserId, newHash, now, _lifetime, ipAddress, userAgent);
            context.RefreshTokens.Add(replacement);

            existing.Revoke(now, replacement.Id);

            await context.SaveChangesAsync(cancellationToken);

            return new RotatedTokens(newRaw, replacement.ExpiresAt, existing.UserId);
        }

        public async Task<Result> RevokeAsync(string rawRefreshToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                return Result.Success();
            }

            string hash = RefreshTokenHasher.Hash(rawRefreshToken);
            RefreshToken? token = await context.RefreshTokens
                .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

            if (token is null || token.IsRevoked)
            {
                return Result.Success();
            }

            token.Revoke(dateTimeProvider.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;

            List<RefreshToken> active = await context.RefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (RefreshToken token in active)
            {
                token.Revoke(now);
            }

            if (active.Count > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        private static string GenerateRawToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
