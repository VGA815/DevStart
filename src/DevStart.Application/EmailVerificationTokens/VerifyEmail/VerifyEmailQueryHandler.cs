using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.EmailVerificationTokens;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.EmailVerificationTokens.VerifyEmail
{
    internal sealed class VerifyEmailQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider) 
        : IQueryHandler<VerifyEmailQuery, EmailVerificationResponse>
    {
        public async Task<Result<EmailVerificationResponse>> Handle(VerifyEmailQuery query, CancellationToken cancellationToken)
        {
            EmailVerificationToken? token = await context.EmailVerificationTokens.FirstOrDefaultAsync(x => x.TokenId == query.TokenId, cancellationToken);
            if (token == null || token.ExpiresAt < dateTimeProvider.UtcNow)
            {
                return Result.Failure<EmailVerificationResponse>(EmailVerificationTokenErrors.NotFound(query.TokenId));
            }
            User? user = await context.Users.FirstOrDefaultAsync(x => x.Id == token.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure<EmailVerificationResponse>(UserErrors.NotFound(token.UserId));
            }

            var emailVerificationResponse = new EmailVerificationResponse
            {
                IsSuccessful = true
            };

            if (user.IsVerified)
            {
                context.EmailVerificationTokens.Remove(token);
                await context.SaveChangesAsync(cancellationToken);
                return emailVerificationResponse;
            }

            user.IsVerified = true;
            context.EmailVerificationTokens.Remove(token);

            await context.SaveChangesAsync(cancellationToken);

            return emailVerificationResponse;
        }
    }
}
