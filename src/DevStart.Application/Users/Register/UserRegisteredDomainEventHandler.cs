using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.EmailVerificationTokens;
using DevStart.Domain.Notifications;
using DevStart.Domain.Users;
using DevStart.SharedKernel;

namespace DevStart.Application.Users.Register
{
    internal sealed class UserRegisteredDomainEventHandler(IApplicationDbContext context, IEmailSender emailSender, IDateTimeProvider dateTimeProvider, INotificationSender notificationSender) : IDomainEventHandler<UserRegisteredDomainEvent>
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
            Notification notification = new()
            {
                Id = Guid.NewGuid(),
                UserId = domainEvent.UserId,
                Type = "Welcome",
                Title = "Welcome to DevStart!",
                Body = "Thank you for registering. Please verify your email address to get started.",
                ReferenceId = null,
                IsRead = false,
                CreatedAt = dateTimeProvider.UtcNow
            };

            context.EmailVerificationTokens.Add(token);
            context.Notifications.Add(notification);
            await context.SaveChangesAsync(cancellationToken);


            await emailSender.SendVerification(domainEvent.Email, token.TokenId.ToString());           
            await notificationSender.SendAsync(notification.Id, notification.UserId, notification.Type, notification.Title, notification.Body, notification.CreatedAt, notification.ReferenceId, cancellationToken);
        }
    }
}
