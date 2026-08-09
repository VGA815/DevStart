using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Auth.OAuth;
using DevStart.Application.Users.Security;
using DevStart.Domain.Security;
using DevStart.Domain.Users;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DevStart.Application.Auth.TwoFactor
{
    internal sealed class TwoFactorLoginGate(
        IApplicationDbContext context,
        IPendingTwoFactorStore pendingStore,
        ITrustedDeviceService trustedDevices,
        IUserSecuritySettingsProvider securitySettings) : ITwoFactorLoginGate
    {
        /// <summary>
        /// Short on purpose: the challenge only bridges the seconds between entering the password
        /// and typing the authenticator code (or scanning the QR during mandatory admin setup).
        /// </summary>
        internal static readonly TimeSpan ChallengeTtl = TimeSpan.FromMinutes(5);

        public async Task<OAuthAuthResult?> ChallengeIfRequiredAsync(
            User user,
            string? ipAddress,
            string? userAgent,
            string? deviceToken,
            CancellationToken cancellationToken)
        {
            bool enabled = await context.UserTwoFactors
                .AnyAsync(t => t.UserId == user.Id && t.IsEnabled, cancellationToken);

            if (!enabled)
            {
                // Deliberately before any device lookup: a trusted device attests that this browser
                // once completed a second factor for this account. An admin who has never enrolled
                // has completed none, so there is nothing for a device to vouch for.
                if (user.Role == UserSystemRole.Admin)
                {
                    string setupToken = await SavePendingAsync(user.Id, ipAddress, userAgent, setupRequired: true, cancellationToken);
                    return OAuthAuthResult.TwoFactorSetupRequired(new TwoFactorSetupChallenge(setupToken));
                }

                return null;
            }

            if (deviceToken is not null)
            {
                UserSecuritySettings settings = await securitySettings.GetOrDefaultAsync(user.Id, cancellationToken);

                // A rejected device token falls through to the ordinary challenge, exactly like a
                // missing one — never a distinct error, which would tell an attacker what they hold.
                if (await trustedDevices.TryConsumeAsync(user, deviceToken, ipAddress, settings.Strictness, cancellationToken))
                {
                    return null;
                }
            }

            string token = await SavePendingAsync(user.Id, ipAddress, userAgent, setupRequired: false, cancellationToken);
            return OAuthAuthResult.TwoFactorRequired(new TwoFactorChallenge(token));
        }

        private async Task<string> SavePendingAsync(
            Guid userId, string? ipAddress, string? userAgent, bool setupRequired, CancellationToken cancellationToken)
        {
            string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            await pendingStore.SaveAsync(
                token,
                new PendingTwoFactorLogin(userId, ipAddress, userAgent, setupRequired),
                ChallengeTtl,
                cancellationToken);
            return token;
        }
    }
}
