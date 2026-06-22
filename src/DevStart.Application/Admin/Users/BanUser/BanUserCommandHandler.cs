using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DevStart.Application.Admin.Users.BanUser
{
    internal sealed class BanUserCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IRefreshTokenService refreshTokenService,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<BanUserCommand>
    {
        public async Task<Result> Handle(BanUserCommand command, CancellationToken cancellationToken)
        {
            Guid adminId = userContext.UserId;
            if (command.UserId == adminId)
            {
                return Result.Failure(UserErrors.CannotBanSelf);
            }

            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound(command.UserId));
            }
            if (user.Role == UserSystemRole.Admin)
            {
                return Result.Failure(UserErrors.CannotBanAdmin);
            }

            DateTime now = dateTimeProvider.UtcNow;
            Result ban = user.Ban(command.Reason, command.ExpiresAt, adminId, now);
            if (ban.IsFailure)
            {
                return ban;
            }

            context.AdminActionLogs.Add(AdminActionLog.Create(
                adminId,
                AdminActionType.BanUser,
                AdminTargetType.User,
                user.Id,
                command.Reason,
                now,
                JsonSerializer.Serialize(new { expiresAt = command.ExpiresAt })));

            // Revoke active sessions so the ban takes effect immediately. This also persists the ban and
            // audit log when there is at least one session; the explicit save below covers the no-session case.
            await refreshTokenService.RevokeAllForUserAsync(user.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
