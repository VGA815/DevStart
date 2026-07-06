using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Auth.OAuth;
using DevStart.Domain.Users;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DevStart.Application.Auth.TwoFactor
{
    internal sealed class TwoFactorLoginGate(
        IApplicationDbContext context,
        IPendingTwoFactorStore pendingStore) : ITwoFactorLoginGate
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
            CancellationToken cancellationToken)
        {
            bool enabled = await context.UserTwoFactors
                .AnyAsync(t => t.UserId == user.Id && t.IsEnabled, cancellationToken);

            if (enabled)
            {
                string token = await SavePendingAsync(user.Id, ipAddress, userAgent, setupRequired: false, cancellationToken);
                return OAuthAuthResult.TwoFactorRequired(new TwoFactorChallenge(token));
            }

            if (user.Role == UserSystemRole.Admin)
            {
                string token = await SavePendingAsync(user.Id, ipAddress, userAgent, setupRequired: true, cancellationToken);
                return OAuthAuthResult.TwoFactorSetupRequired(new TwoFactorSetupChallenge(token));
            }

            return null;
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
