using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Auth.Sessions;
using DevStart.Application.Configuration;
using DevStart.Application.Users.Security;
using DevStart.Domain.Security;
using DevStart.Domain.TrustedDevices;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevStart.Infrastructure.Authentication.TrustedDevices
{
    internal sealed class TrustedDeviceService(
        IApplicationDbContext context,
        IUserSecuritySettingsProvider securitySettings,
        IDateTimeProvider dateTimeProvider,
        IOptions<TrustedDeviceOptions> options,
        ILogger<TrustedDeviceService> logger) : ITrustedDeviceService
    {
        private readonly TrustedDeviceOptions _options = options.Value;

        public async Task<bool> TryConsumeAsync(
            User user,
            string? rawToken,
            string? ipAddress,
            TwoFactorStrictness strictness,
            CancellationToken cancellationToken)
        {
            if (!_options.Enabled
                || !AllowsDeviceBypass(strictness)
                || !TrustedDeviceTokenHasher.IsWellFormed(rawToken))
            {
                return false;
            }

            string hash = TrustedDeviceTokenHasher.Hash(rawToken!);
            DateTime now = dateTimeProvider.UtcNow;

            TrustedDevice? device = await context.TrustedDevices
                .FirstOrDefaultAsync(d => d.TokenHash == hash, cancellationToken);

            if (device is null)
            {
                return false;
            }

            // A token that resolves to someone else's device is not a mistake a real client makes —
            // treat it as a compromised or copied token and burn the row.
            if (device.UserId != user.Id)
            {
                logger.LogWarning(
                    "Trusted device token presented for the wrong user; revoking device {DeviceId} of user {UserId}",
                    device.Id, device.UserId);
                device.Revoke(now);
                await context.SaveChangesAsync(cancellationToken);
                return false;
            }

            if (!device.IsActive(now))
            {
                return false;
            }

            if (strictness == TwoFactorStrictness.SameNetworkOnly
                && !IpSubnet.SameNetwork(device.CreatedByIp, ipAddress))
            {
                // Not a revocation: the user may simply be on a different network today.
                return false;
            }

            device.Touch(now, ipAddress);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IssuedTrustedDevice?> IssueAsync(
            User user,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            if (!_options.Enabled)
            {
                return null;
            }

            UserSecuritySettings settings = await securitySettings.GetOrDefaultAsync(user.Id, cancellationToken);
            if (!AllowsDeviceBypass(settings.Strictness))
            {
                return null;
            }

            DateTime now = dateTimeProvider.UtcNow;
            int days = ResolveTrustDays(user, settings.TrustDurationDays);

            await EvictOverflowAsync(user.Id, now, cancellationToken);

            string raw = TrustedDeviceTokenHasher.Generate();
            TrustedDevice device = TrustedDevice.Create(
                user.Id,
                TrustedDeviceTokenHasher.Hash(raw),
                now,
                TimeSpan.FromDays(days),
                ipAddress,
                userAgent,
                UserAgentParser.Parse(userAgent).Label);

            context.TrustedDevices.Add(device);
            await context.SaveChangesAsync(cancellationToken);

            return new IssuedTrustedDevice(raw, device.Id, device.ExpiresAt);
        }

        public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;

            List<TrustedDevice> active = await context.TrustedDevices
                .Where(d => d.UserId == userId && d.RevokedAt == null)
                .ToListAsync(cancellationToken);

            if (active.Count == 0)
            {
                return;
            }

            foreach (TrustedDevice device in active)
            {
                device.Revoke(now);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Which policies may skip the second factor at all — an allow-list, not a deny-list, so an
        /// unrecognized value fails closed. A deny-list would let a value outside the enum (a legacy
        /// or corrupted row, a future member reaching an older deployment mid-rollout) slip past the
        /// EveryLogin and SameNetworkOnly checks and land on the permissive branch.
        /// </summary>
        private static bool AllowsDeviceBypass(TwoFactorStrictness strictness)
            => strictness is TwoFactorStrictness.RememberDevice or TwoFactorStrictness.SameNetworkOnly;

        /// <summary>The user's choice, capped by config — admins get the shorter ceiling.</summary>
        private int ResolveTrustDays(User user, int chosenDays)
        {
            int cap = user.Role == UserSystemRole.Admin ? _options.AdminMaxTrustDays : _options.MaxTrustDays;
            cap = Math.Max(cap, 1);
            return Math.Clamp(Math.Min(chosenDays, cap), 1, cap);
        }

        /// <summary>Keeps the devices list bounded: a scripted client must not be able to grow it forever.</summary>
        private async Task EvictOverflowAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
        {
            List<TrustedDevice> active = await context.TrustedDevices
                .Where(d => d.UserId == userId && d.RevokedAt == null && d.ExpiresAt > now)
                .OrderByDescending(d => d.LastUsedAt)
                .ToListAsync(cancellationToken);

            int limit = Math.Max(_options.MaxDevicesPerUser, 1);
            for (int i = limit - 1; i < active.Count; i++)
            {
                active[i].Revoke(now);
            }
        }
    }
}
