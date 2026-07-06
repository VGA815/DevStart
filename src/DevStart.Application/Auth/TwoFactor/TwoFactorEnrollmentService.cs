using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.TwoFactor;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Auth.TwoFactor
{
    internal sealed class TwoFactorEnrollmentService(
        IApplicationDbContext context,
        ITotpProvider totpProvider,
        ITwoFactorSecretProtector secretProtector,
        IRecoveryCodeGenerator recoveryCodeGenerator,
        IDateTimeProvider dateTimeProvider) : ITwoFactorEnrollmentService
    {
        internal const int RecoveryCodeCount = 10;

        public async Task<Result<TwoFactorSetupData>> StartAsync(User user, CancellationToken cancellationToken)
        {
            UserTwoFactor? twoFactor = await context.UserTwoFactors
                .SingleOrDefaultAsync(t => t.UserId == user.Id, cancellationToken);

            if (twoFactor?.IsEnabled == true)
            {
                return Result.Failure<TwoFactorSetupData>(TwoFactorErrors.AlreadyEnabled);
            }

            DateTime now = dateTimeProvider.UtcNow;
            string secret = totpProvider.GenerateSecret();
            string encryptedSecret = secretProtector.Protect(secret);

            if (twoFactor is null)
            {
                context.UserTwoFactors.Add(UserTwoFactor.CreatePending(user.Id, encryptedSecret, now));
            }
            else
            {
                Result rotated = twoFactor.RotatePendingSecret(encryptedSecret, now);
                if (rotated.IsFailure)
                {
                    return Result.Failure<TwoFactorSetupData>(rotated.Error);
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            return new TwoFactorSetupData(secret, totpProvider.BuildOtpAuthUri(secret, user.Email));
        }

        public async Task<Result<IReadOnlyList<string>>> ConfirmAsync(
            Guid userId, string code, CancellationToken cancellationToken)
        {
            UserTwoFactor? twoFactor = await context.UserTwoFactors
                .SingleOrDefaultAsync(t => t.UserId == userId, cancellationToken);

            if (twoFactor is null)
            {
                return Result.Failure<IReadOnlyList<string>>(TwoFactorErrors.SetupNotStarted);
            }
            if (twoFactor.IsEnabled)
            {
                return Result.Failure<IReadOnlyList<string>>(TwoFactorErrors.AlreadyEnabled);
            }

            string secret = secretProtector.Unprotect(twoFactor.EncryptedSecret);
            if (!totpProvider.VerifyCode(secret, code.Trim(), lastUsedTimestep: null, out long matchedTimestep))
            {
                return Result.Failure<IReadOnlyList<string>>(TwoFactorErrors.InvalidCode);
            }

            DateTime now = dateTimeProvider.UtcNow;
            Result enabled = twoFactor.Enable(matchedTimestep, now);
            if (enabled.IsFailure)
            {
                return Result.Failure<IReadOnlyList<string>>(enabled.Error);
            }

            // Codes left over from a previous enrollment must not survive a re-enrollment.
            List<TwoFactorRecoveryCode> staleCodes = await context.TwoFactorRecoveryCodes
                .Where(c => c.UserId == userId)
                .ToListAsync(cancellationToken);
            context.TwoFactorRecoveryCodes.RemoveRange(staleCodes);

            IReadOnlyList<string> codes = recoveryCodeGenerator.Generate(RecoveryCodeCount);
            foreach (string plaintext in codes)
            {
                context.TwoFactorRecoveryCodes.Add(
                    TwoFactorRecoveryCode.Create(userId, recoveryCodeGenerator.Hash(plaintext), now));
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(codes);
        }
    }
}
