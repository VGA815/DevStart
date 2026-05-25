using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.ChangePassword
{
    internal sealed class ChangePasswordCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IRefreshTokenService refreshTokenService)
        : ICommandHandler<ChangePasswordCommand>
    {
        public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
        {
            User? user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound(userContext.UserId));
            }

            // OAuth-only accounts have no password to verify; direct them to the reset flow to set one.
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                return Result.Failure(UserErrors.PasswordNotSet);
            }

            if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
            {
                return Result.Failure(UserErrors.InvalidCurrentPassword);
            }

            user.PasswordHash = passwordHasher.Hash(command.NewPassword);
            user.UpdatedAt = dateTimeProvider.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            // Force re-authentication everywhere else after a credential change.
            await refreshTokenService.RevokeAllForUserAsync(user.Id, cancellationToken);

            return Result.Success();
        }
    }
}
