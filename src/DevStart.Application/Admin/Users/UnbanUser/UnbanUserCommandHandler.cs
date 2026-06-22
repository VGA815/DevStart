using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Users.UnbanUser
{
    internal sealed class UnbanUserCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<UnbanUserCommand>
    {
        public async Task<Result> Handle(UnbanUserCommand command, CancellationToken cancellationToken)
        {
            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound(command.UserId));
            }

            DateTime now = dateTimeProvider.UtcNow;
            Result unban = user.Unban(now);
            if (unban.IsFailure)
            {
                return unban;
            }

            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.UnbanUser,
                AdminTargetType.User,
                user.Id,
                command.Reason ?? "Unbanned",
                now));

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
