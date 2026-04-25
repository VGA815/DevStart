using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.Notifications;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentApplications.Withdraw
{
    internal sealed class InvestmentApplicationWithdrawnDomainEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<InvestmentApplicationWithdrawnDomainEvent>
    {
        public async Task Handle(InvestmentApplicationWithdrawnDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            List<Guid> recipientIds = await context.StartupMembers
                .AsNoTracking()
                .Where(sm => sm.StartupId == domainEvent.StartupId
                          && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration))
                .Select(sm => sm.ProfileId)
                .ToListAsync(cancellationToken);

            foreach (Guid recipientId in recipientIds)
            {
                Notification notification = Notification.Create(
                    userId: recipientId,
                    type: NotificationType.InvestmentApplicationWithdrawn,
                    title: "Investment application withdrawn",
                    body: "An investor has withdrawn their investment application.",
                    createdAt: dateTimeProvider.UtcNow,
                    referenceId: domainEvent.ApplicationId);

                await notificationService.PublishAsync(notification, cancellationToken);
            }
        }
    }
}
