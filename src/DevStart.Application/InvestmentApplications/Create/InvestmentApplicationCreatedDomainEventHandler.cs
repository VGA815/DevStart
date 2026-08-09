using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.Notifications;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentApplications.Create
{
    internal sealed class InvestmentApplicationCreatedDomainEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<InvestmentApplicationCreatedDomainEvent>
    {
        public async Task Handle(InvestmentApplicationCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
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
                type: NotificationType.InvestmentApplicationReceived,
                title: "New investment application",
                body: "You have received a new investment application.",
                createdAt: utcNow,
                referenceId: domainEvent.ApplicationId))];

            await notificationService.PublishManyAsync(notifications, cancellationToken);
        }
    }
}
