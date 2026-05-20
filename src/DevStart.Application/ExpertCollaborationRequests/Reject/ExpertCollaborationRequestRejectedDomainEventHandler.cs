using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Notifications;
using DevStart.SharedKernel;

namespace DevStart.Application.ExpertCollaborationRequests.Reject
{
    internal sealed class ExpertCollaborationRequestRejectedDomainEventHandler(
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<ExpertCollaborationRequestRejectedDomainEvent>
    {
        public async Task Handle(ExpertCollaborationRequestRejectedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            Notification notification = Notification.Create(
                userId: domainEvent.ExpertProfileId,
                type: NotificationType.ExpertCollaborationRequestRejected,
                title: "Collaboration request rejected",
                body: "Your collaboration request has been rejected.",
                createdAt: dateTimeProvider.UtcNow,
                referenceId: domainEvent.RequestId);

            await notificationService.PublishAsync(notification, cancellationToken);
        }
    }
}
