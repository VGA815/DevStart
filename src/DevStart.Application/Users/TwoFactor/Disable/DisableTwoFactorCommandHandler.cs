using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.TwoFactor;
using DevStart.Domain.TwoFactor;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.TwoFactor.Disable
{
    internal sealed class DisableTwoFactorCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IPasswordHasher passwordHasher,
        ITwoFactorCodeVerifier codeVerifier,
        IRefreshTokenService refreshTokenService) : ICommandHandler<DisableTwoFactorCommand>
    {
        public async Task<Result> Handle(DisableTwoFactorCommand command, CancellationToken cancellationToken)
        {
            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound(userContext.UserId));
            }

            UserTwoFactor? twoFactor = await context.UserTwoFactors
                .SingleOrDefaultAsync(t => t.UserId == user.Id, cancellationToken);
            if (twoFactor is null || !twoFactor.IsEnabled)
            {
                return Result.Failure(TwoFactorErrors.NotEnabled);
            }

            // OAuth-only accounts have no password; the second factor alone authorizes the change.
            if (user.HasPassword &&
                (string.IsNullOrEmpty(command.Password) || !passwordHasher.Verify(command.Password, user.PasswordHash!)))
            {
                return Result.Failure(UserErrors.InvalidCurrentPassword);
            }

            if (!await codeVerifier.VerifyAndConsumeAsync(twoFactor, command.Code, cancellationToken))
            {
                return Result.Failure(TwoFactorErrors.InvalidCode);
            }

            List<TwoFactorRecoveryCode> codes = await context.TwoFactorRecoveryCodes
                .Where(c => c.UserId == user.Id)
                .ToListAsync(cancellationToken);
            context.TwoFactorRecoveryCodes.RemoveRange(codes);
            context.UserTwoFactors.Remove(twoFactor);

            // Raised on the (tracked) user: events on the deleted row would be lost after save.
            user.Raise(new TwoFactorDisabledDomainEvent(user.Id, ResetByAdmin: false));

            await context.SaveChangesAsync(cancellationToken);

            // Credential change: force re-authentication everywhere.
            await refreshTokenService.RevokeAllForUserAsync(user.Id, cancellationToken);

            return Result.Success();
        }
    }
}
