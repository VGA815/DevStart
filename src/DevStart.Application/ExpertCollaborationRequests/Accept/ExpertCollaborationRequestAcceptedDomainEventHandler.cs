using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Notifications;
using DevStart.SharedKernel;

namespace DevStart.Application.ExpertCollaborationRequests.Accept
{
    internal sealed class ExpertCollaborationRequestAcceptedDomainEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<ExpertCollaborationRequestAcceptedDomainEvent>
    {
        public async Task Handle(ExpertCollaborationRequestAcceptedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            // The answer goes back to whoever opened the request.
            List<Guid> recipientIds = await ExpertCollaborationRequestParticipants.GetInitiatorRecipientsAsync(
                context,
                domainEvent.StartupId,
                domainEvent.ExpertProfileId,
                domainEvent.Initiator,
                cancellationToken);

            (NotificationType type, string title, string body) = ExpertCollaborationNotifications
                .Accepted(domainEvent.Initiator);

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
