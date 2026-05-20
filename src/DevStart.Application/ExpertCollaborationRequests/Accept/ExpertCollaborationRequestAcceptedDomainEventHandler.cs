using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Notifications;
using DevStart.SharedKernel;

namespace DevStart.Application.ExpertCollaborationRequests.Accept
{
    internal sealed class ExpertCollaborationRequestAcceptedDomainEventHandler(
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<ExpertCollaborationRequestAcceptedDomainEvent>
    {
        public async Task Handle(ExpertCollaborationRequestAcceptedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            Notification notification = Notification.Create(
                userId: domainEvent.ExpertProfileId,
                type: NotificationType.ExpertCollaborationRequestAccepted,
                title: "Collaboration request accepted",
                body: "Your collaboration request has been accepted.",
                createdAt: dateTimeProvider.UtcNow,
                referenceId: domainEvent.RequestId);

            await notificationService.PublishAsync(notification, cancellationToken);
        }
    }
}
