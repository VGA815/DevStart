using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.TwoFactor;
using DevStart.Domain.TwoFactor;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.TwoFactor.RegenerateRecoveryCodes
{
    internal sealed class RegenerateRecoveryCodesCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ITotpProvider totpProvider,
        ITwoFactorSecretProtector secretProtector,
        IRecoveryCodeGenerator recoveryCodeGenerator,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<RegenerateRecoveryCodesCommand, IReadOnlyList<string>>
    {
        public async Task<Result<IReadOnlyList<string>>> Handle(
            RegenerateRecoveryCodesCommand command, CancellationToken cancellationToken)
        {
            UserTwoFactor? twoFactor = await context.UserTwoFactors
                .SingleOrDefaultAsync(t => t.UserId == userContext.UserId, cancellationToken);
            if (twoFactor is null || !twoFactor.IsEnabled)
            {
                return Result.Failure<IReadOnlyList<string>>(TwoFactorErrors.NotEnabled);
            }

            DateTime now = dateTimeProvider.UtcNow;
            string trimmed = command.Code.Trim();

            // TOTP only: a leaked recovery code must not be able to mint a fresh set.
            if (!TwoFactorCodeVerifier.IsTotpShaped(trimmed))
            {
                return Result.Failure<IReadOnlyList<string>>(TwoFactorErrors.InvalidCode);
            }

            string secret = secretProtector.Unprotect(twoFactor.EncryptedSecret);
            if (!totpProvider.VerifyCode(secret, trimmed, twoFactor.LastUsedTimestep, out long matchedTimestep) ||
                !twoFactor.TryAcceptTimestep(matchedTimestep, now))
            {
                return Result.Failure<IReadOnlyList<string>>(TwoFactorErrors.InvalidCode);
            }

            List<TwoFactorRecoveryCode> oldCodes = await context.TwoFactorRecoveryCodes
                .Where(c => c.UserId == userContext.UserId)
                .ToListAsync(cancellationToken);
            context.TwoFactorRecoveryCodes.RemoveRange(oldCodes);

            IReadOnlyList<string> codes = recoveryCodeGenerator.Generate(TwoFactorEnrollmentService.RecoveryCodeCount);
            foreach (string plaintext in codes)
            {
                context.TwoFactorRecoveryCodes.Add(
                    TwoFactorRecoveryCode.Create(userContext.UserId, recoveryCodeGenerator.Hash(plaintext), now));
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(codes);
        }
    }
}
