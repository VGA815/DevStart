using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.PasswordResetTokens;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.ForgotPassword
{
    internal sealed class ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IEmailSender emailSender,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<ForgotPasswordCommand>
    {
        public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
        {
            User? user = await context.Users
                .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

            // Enumeration-safe: always succeed regardless of whether the email is registered, so
            // callers can't probe which addresses have accounts. OAuth-only users (no password yet)
            // are allowed through — the reset flow lets them set their first password.
            if (user is null)
            {
                return Result.Success();
            }

            // Throttle resets to avoid email-bombing: skip silently if a token was issued < 60s ago.
            PasswordResetToken? latest = await context.PasswordResetTokens
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (latest is not null && latest.CreatedAt > dateTimeProvider.UtcNow - TimeSpan.FromSeconds(60))
            {
                return Result.Success();
            }

            var oldTokens = context.PasswordResetTokens.Where(t => t.UserId == user.Id);
            context.PasswordResetTokens.RemoveRange(oldTokens);

            PasswordResetToken token = PasswordResetToken.Create(
                user.Id,
                dateTimeProvider.UtcNow,
                dateTimeProvider.UtcNow + TimeSpan.FromMinutes(30));
            context.PasswordResetTokens.Add(token);

            await context.SaveChangesAsync(cancellationToken);
            await emailSender.SendPasswordReset(user.Email, token.TokenId.ToString());
            return Result.Success();
        }
    }
}
