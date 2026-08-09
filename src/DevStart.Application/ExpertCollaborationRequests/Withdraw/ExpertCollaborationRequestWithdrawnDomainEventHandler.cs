using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Notifications;
using DevStart.SharedKernel;

namespace DevStart.Application.ExpertCollaborationRequests.Withdraw
{
    internal sealed class ExpertCollaborationRequestWithdrawnDomainEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<ExpertCollaborationRequestWithdrawnDomainEvent>
    {
        public async Task Handle(ExpertCollaborationRequestWithdrawnDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            // The side that was waiting to answer is the one whose inbox just changed.
            List<Guid> recipientIds = await ExpertCollaborationRequestParticipants.GetResponderRecipientsAsync(
                context,
                domainEvent.StartupId,
                domainEvent.ExpertProfileId,
                domainEvent.Initiator,
                cancellationToken);

            (NotificationType type, string title, string body) = ExpertCollaborationNotifications
                .Withdrawn(domainEvent.Initiator);

            DateTime utcNow = dateTimeProvider.UtcNow;

            List<Notification> notifications = [.. recipientIds.Select(recipientId => Notification.Create(
                userId: recipientId,
                type: type,
                title: title,
                body: body,
                createdAt: utcNow,
                referenceId: domainEvent.RequestId))];

            await notificationService.PublishManyAsync(notifications, cancellationToken);
        }
    }
}
