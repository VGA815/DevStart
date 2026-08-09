using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Notifications;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertCollaborationRequests.Create
{
    internal sealed class ExpertCollaborationRequestCreatedDomainEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<ExpertCollaborationRequestCreatedDomainEvent>
    {
        public async Task Handle(ExpertCollaborationRequestCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            List<Guid> recipientIds = await context.StartupMembers
                .AsNoTracking()
                .Where(sm => sm.StartupId == domainEvent.StartupId
                          && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration))
                .Select(sm => sm.ProfileId)
                .ToListAsync(cancellationToken);

            DateTime utcNow = dateTimeProvider.UtcNow;

            List<Notification> notifications = [.. recipientIds.Select(recipientId => Notification.Create(
                userId: recipientId,
                type: NotificationType.ExpertCollaborationRequestReceived,
                title: "New expert collaboration request",
                body: "You have received a new collaboration request from an expert.",
                createdAt: utcNow,
                referenceId: domainEvent.RequestId))];

            await notificationService.PublishManyAsync(notifications, cancellationToken);
        }
    }
}
