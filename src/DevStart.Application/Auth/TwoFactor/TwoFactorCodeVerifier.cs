using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.TwoFactor;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Auth.TwoFactor
{
    internal sealed class TwoFactorCodeVerifier(
        IApplicationDbContext context,
        ITotpProvider totpProvider,
        ITwoFactorSecretProtector secretProtector,
        IRecoveryCodeGenerator recoveryCodeGenerator,
        IDateTimeProvider dateTimeProvider) : ITwoFactorCodeVerifier
    {
        public async Task<bool> VerifyAndConsumeAsync(
            UserTwoFactor twoFactor, string code, CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;
            string trimmed = code.Trim();

            if (IsTotpShaped(trimmed))
            {
                string secret = secretProtector.Unprotect(twoFactor.EncryptedSecret);
                return totpProvider.VerifyCode(secret, trimmed, twoFactor.LastUsedTimestep, out long matchedTimestep)
                    && twoFactor.TryAcceptTimestep(matchedTimestep, now);
            }

            // Anything that is not 6 digits is treated as a recovery code. Lookup is by SHA-256
            // hash (same approach as refresh tokens): the input is hashed before comparison, so the
            // string equality below leaks nothing usable about stored codes.
            string hash = recoveryCodeGenerator.Hash(trimmed);
            TwoFactorRecoveryCode? match = await context.TwoFactorRecoveryCodes
                .SingleOrDefaultAsync(
                    c => c.UserId == twoFactor.UserId && c.CodeHash == hash && c.UsedAt == null,
                    cancellationToken);

            if (match is null)
            {
                return false;
            }

            match.MarkUsed(now);
            return true;
        }

        internal static bool IsTotpShaped(string code) =>
            code.Length == 6 && code.All(char.IsAsciiDigit);
    }
}
