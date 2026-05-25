using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.EmailVerificationTokens;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.EmailVerificationTokens.ResendEmailVerification
{
    internal sealed class ResendEmailVerificationCommandHandler(IApplicationDbContext context, IEmailSender emailSender, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<ResendEmailVerificationCommand>
    {
        public async Task<Result> Handle(ResendEmailVerificationCommand command, CancellationToken cancellationToken)
        {
            User? user = await context.Users.SingleOrDefaultAsync(x => x.Email == command.Email, cancellationToken);
            if (user == null)
            {
                return Result.Failure(UserErrors.NotFoundByEmail);
            }

            if (user.IsVerified)
            {
                return Result.Failure(UserErrors.AlreadyVerified);
            }

            // Throttle resends to avoid email-bombing: refuse if a token was issued < 60s ago.
            EmailVerificationToken? latest = await context.EmailVerificationTokens
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (latest is not null && latest.CreatedAt > dateTimeProvider.UtcNow - TimeSpan.FromSeconds(60))
            {
                return Result.Failure(EmailVerificationTokenErrors.ResendTooSoon);
            }

            var oldTokens = context.EmailVerificationTokens.Where(t => t.UserId == user.Id);
            context.EmailVerificationTokens.RemoveRange(oldTokens);

            EmailVerificationToken token = new()
            {
                CreatedAt = dateTimeProvider.UtcNow,
                ExpiresAt = dateTimeProvider.UtcNow + TimeSpan.FromMinutes(20),
                TokenId = Guid.NewGuid(),
                UserId = user.Id,
            };
            context.EmailVerificationTokens.Add(token);

            await context.SaveChangesAsync(cancellationToken);
            await emailSender.SendVerification(user.Email, token.TokenId.ToString());
            return Result.Success();
        }
    }
}
