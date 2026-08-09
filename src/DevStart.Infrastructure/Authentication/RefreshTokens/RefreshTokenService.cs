using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Auth.Sessions;
using DevStart.Application.Users.Security;
using DevStart.Domain.RefreshTokens;
using DevStart.Domain.Security;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace DevStart.Infrastructure.Authentication.RefreshTokens
{
    internal sealed class RefreshTokenService(
        IApplicationDbContext context,
        ITrustedDeviceService trustedDevices,
        IUserSecuritySettingsProvider securitySettings,
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

            bool notifyNewDevice = await IsNewDeviceAsync(user.Id, userAgent, now, cancellationToken);

            RefreshToken token = RefreshToken.Create(user.Id, hash, now, _lifetime, ipAddress, userAgent);

            if (notifyNewDevice)
            {
                // Raised from Infrastructure rather than the four Application handlers that issue
                // tokens: this method is the single funnel for "a session was created", so raising it
                // here is the only way the check cannot be forgotten at a future call site.
                UserAgentInfo parsed = UserAgentParser.Parse(userAgent);
                token.Raise(new NewDeviceLoginDomainEvent(
                    user.Id, user.Email, parsed.Browser, parsed.Os, ipAddress, now));
            }

            context.RefreshTokens.Add(token);

            await context.SaveChangesAsync(cancellationToken);

            return new IssuedRefreshToken(raw, token.ExpiresAt, token.SessionId);
        }

        /// <summary>
        /// Deliberately coarse — it matches on browser/OS, not a fingerprint, so a colleague on the
        /// same Chrome/Windows will not trigger an alert. The goal is to catch a sign-in from
        /// somewhere the user has plainly never been, without turning every login into an email.
        /// </summary>
        private async Task<bool> IsNewDeviceAsync(
            Guid userId, string? userAgent, DateTime now, CancellationToken cancellationToken)
        {
            // API clients and tests send no User-Agent; there is nothing meaningful to report.
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return false;
            }

            bool hasHistory = await context.RefreshTokens
                .AnyAsync(t => t.UserId == userId, cancellationToken);

            // First session ever: the user is at the keyboard, having just registered.
            if (!hasHistory)
            {
                return false;
            }

            UserSecuritySettings settings = await securitySettings.GetOrDefaultAsync(userId, cancellationToken);
            if (!settings.NotifyOnNewDeviceLogin)
            {
                return false;
            }

            UserAgentInfo parsed = UserAgentParser.Parse(userAgent);
            DateTime since = now - SessionRetentionPolicy.KnownDeviceLookback;

            // The refresh_tokens rows are already a free "devices seen recently" list.
            List<string?> recentAgents = await context.RefreshTokens
                .Where(t => t.UserId == userId && t.CreatedAt >= since && t.UserAgent != null)
                .Select(t => t.UserAgent)
                .Distinct()
                .ToListAsync(cancellationToken);

            return !recentAgents.Any(a =>
            {
                UserAgentInfo known = UserAgentParser.Parse(a);
                return known.Browser == parsed.Browser && known.Os == parsed.Os;
            });
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
                // Only a *superseded* token replayed after rotation is evidence of theft. A token
                // revoked deliberately — logout, "end this session", "sign out everywhere" — has no
                // replacement, and treating its next use as an attack would mean ending one session
                // from the settings screen silently signs the user out of every other one too.
                if (existing.ReplacedByTokenId is not null)
                {
                    await RevokeAllForUserAsync(existing.UserId, cancellationToken);
                    return Result.Failure<RotatedTokens>(RefreshTokenErrors.ReuseDetected);
                }

                return Result.Failure<RotatedTokens>(RefreshTokenErrors.Invalid);
            }

            if (existing.IsExpired(now))
            {
                return Result.Failure<RotatedTokens>(RefreshTokenErrors.Expired);
            }

            string newRaw = GenerateRawToken();
            string newHash = RefreshTokenHasher.Hash(newRaw);

            RefreshToken replacement = RefreshToken.CreateReplacement(
                existing, newHash, now, _lifetime, ipAddress, userAgent);
            context.RefreshTokens.Add(replacement);

            existing.Revoke(now, replacement.Id);

            await context.SaveChangesAsync(cancellationToken);

            return new RotatedTokens(newRaw, replacement.ExpiresAt, existing.UserId, replacement.SessionId);
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

            // Every caller of this method is invalidating the user's credentials in some way; a device
            // that could still skip the second factor would quietly undo that. Doing it here rather
            // than at each call site makes it impossible to forget at the next one.
            await trustedDevices.RevokeAllForUserAsync(userId, cancellationToken);
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
