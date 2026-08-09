using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Notifications;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertCollaborationRequests.Withdraw
{
    internal sealed class ExpertCollaborationRequestWithdrawnDomainEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<ExpertCollaborationRequestWithdrawnDomainEvent>
    {
        public async Task Handle(ExpertCollaborationRequestWithdrawnDomainEvent domainEvent, CancellationToken cancellationToken)
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
                type: NotificationType.ExpertCollaborationRequestWithdrawn,
                title: "Collaboration request withdrawn",
                body: "An expert has withdrawn their collaboration request.",
                createdAt: utcNow,
                referenceId: domainEvent.RequestId))];

            await notificationService.PublishManyAsync(notifications, cancellationToken);
        }
    }
}
