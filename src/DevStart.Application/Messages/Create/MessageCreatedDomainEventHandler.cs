using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.Messages;
using DevStart.Domain.Notifications;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Messages.Create
{
    internal sealed class MessageCreatedDomainEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<MessageCreatedDomainEvent>
    {
        public async Task Handle(MessageCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            if (domainEvent.ReceiverType == ChatParticipantType.User)
            {
                await PublishOneAsync(domainEvent.ReceiverId, domainEvent.MessageId, cancellationToken);
                return;
            }

            // Only the people who may open the startup's chat are told about it.
            List<Guid> memberProfileIds = await context.StartupMembers
                .AsNoTracking()
                .Where(sm => sm.StartupId == domainEvent.ReceiverId
                          && MessagingRoles.CanActAsStartup.Contains(sm.Role))
                .Select(sm => sm.ProfileId)
                .ToListAsync(cancellationToken);

            foreach (Guid profileId in memberProfileIds)
            {
                if (domainEvent.SenderType == ChatParticipantType.User && profileId == domainEvent.SenderId)
                {
                    continue;
                }

                // Someone who leads both sides must not be pinged about their own message.
                if (profileId == domainEvent.SentByProfileId)
                {
                    continue;
                }

                await PublishOneAsync(profileId, domainEvent.MessageId, cancellationToken);
            }
        }

        private Task PublishOneAsync(Guid userId, Guid messageId, CancellationToken cancellationToken)
        {
            Notification notification = Notification.Create(
                userId: userId,
                type: NotificationType.MessageReceived,
                title: "New message",
                body: "You have received a new message.",
                createdAt: dateTimeProvider.UtcNow,
                referenceId: messageId);

            return notificationService.PublishAsync(notification, cancellationToken);
        }
    }
}
