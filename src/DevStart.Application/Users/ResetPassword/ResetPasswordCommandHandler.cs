using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.PasswordResetTokens;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.ResetPassword
{
    internal sealed class ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IRefreshTokenService refreshTokenService)
        : ICommandHandler<ResetPasswordCommand>
    {
        public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
        {
            PasswordResetToken? token = await context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.TokenId == command.TokenId, cancellationToken);

            if (token is null || token.ExpiresAt < dateTimeProvider.UtcNow)
            {
                return Result.Failure(PasswordResetTokenErrors.NotFound(command.TokenId));
            }

            User? user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);

            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound(token.UserId));
            }

            user.PasswordHash = passwordHasher.Hash(command.NewPassword);
            user.UpdatedAt = dateTimeProvider.UtcNow;

            // Single-use: drop this token and any other outstanding reset tokens for the user.
            var userTokens = context.PasswordResetTokens.Where(t => t.UserId == user.Id);
            context.PasswordResetTokens.RemoveRange(userTokens);

            await context.SaveChangesAsync(cancellationToken);

            // A reset often follows an account compromise, so invalidate every existing session.
            await refreshTokenService.RevokeAllForUserAsync(user.Id, cancellationToken);

            return Result.Success();
        }
    }
}
