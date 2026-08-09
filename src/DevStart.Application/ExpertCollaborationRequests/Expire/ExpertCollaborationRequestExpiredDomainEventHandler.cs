using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Notifications;
using DevStart.SharedKernel;

namespace DevStart.Application.ExpertCollaborationRequests.Expire
{
    internal sealed class ExpertCollaborationRequestExpiredDomainEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<ExpertCollaborationRequestExpiredDomainEvent>
    {
        public async Task Handle(ExpertCollaborationRequestExpiredDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            // Only the side that was waiting for an answer is told. The side that never responded does
            // not need a notification about the thing it ignored.
            List<Guid> recipientIds = await ExpertCollaborationRequestParticipants.GetInitiatorRecipientsAsync(
                context,
                domainEvent.StartupId,
                domainEvent.ExpertProfileId,
                domainEvent.Initiator,
                cancellationToken);

            (NotificationType type, string title, string body) = ExpertCollaborationNotifications
                .Expired(domainEvent.Initiator);

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
