using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.Domain.TwoFactor;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Users.ResetTwoFactor
{
    /// <summary>
    /// Support action for a user locked out of 2FA (lost device and recovery codes). Wipes the
    /// TOTP secret and recovery codes, revokes all sessions and leaves an audit trail. An admin
    /// target will be forced back into enrollment at the next login (mandatory-2FA rule).
    /// </summary>
    internal sealed class ResetUserTwoFactorCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IRefreshTokenService refreshTokenService,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<ResetUserTwoFactorCommand>
    {
        public async Task<Result> Handle(ResetUserTwoFactorCommand command, CancellationToken cancellationToken)
        {
            Guid adminId = userContext.UserId;
            if (command.UserId == adminId)
            {
                return Result.Failure(TwoFactorErrors.CannotResetSelf);
            }

            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound(command.UserId));
            }

            UserTwoFactor? twoFactor = await context.UserTwoFactors
                .SingleOrDefaultAsync(t => t.UserId == command.UserId, cancellationToken);
            if (twoFactor is null)
            {
                return Result.Failure(TwoFactorErrors.NotEnabled);
            }

            List<TwoFactorRecoveryCode> codes = await context.TwoFactorRecoveryCodes
                .Where(c => c.UserId == command.UserId)
                .ToListAsync(cancellationToken);
            context.TwoFactorRecoveryCodes.RemoveRange(codes);
            context.UserTwoFactors.Remove(twoFactor);

            // Raised on the (tracked) user: events on the deleted row would be lost after save.
            user.Raise(new TwoFactorDisabledDomainEvent(user.Id, ResetByAdmin: true));

            DateTime now = dateTimeProvider.UtcNow;
            context.AdminActionLogs.Add(AdminActionLog.Create(
                adminId,
                AdminActionType.ResetUserTwoFactor,
                AdminTargetType.User,
                user.Id,
                command.Reason,
                now));

            await refreshTokenService.RevokeAllForUserAsync(user.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
