using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.EmailVerificationTokens;
using DevStart.Domain.Users;
using DevStart.SharedKernel;

namespace DevStart.Application.Users.Register
{
    internal sealed class UserRegisteredDomainEventHandler(IApplicationDbContext context, IEmailSender emailSender, IDateTimeProvider dateTimeProvider) : IDomainEventHandler<UserRegisteredDomainEvent>
    {
        public async Task Handle(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            EmailVerificationToken token = new()
            {
                TokenId = Guid.NewGuid(),
                UserId = domainEvent.UserId,
                CreatedAt = dateTimeProvider.UtcNow,
                ExpiresAt = dateTimeProvider.UtcNow.AddMinutes(20)
            };

            context.EmailVerificationTokens.Add(token);
            await context.SaveChangesAsync(cancellationToken);
            await emailSender.SendVerification(domainEvent.Email, token.TokenId.ToString());
        }
    }
}
