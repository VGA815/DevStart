using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.Notifications;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentDeals.Completed
{
    internal sealed class InvestmentDealCompletedDomainEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<InvestmentDealCompletedDomainEvent>
    {
        public async Task Handle(InvestmentDealCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            DateTime utcNow = dateTimeProvider.UtcNow;

            List<Guid> recipientIds = await context.StartupMembers
                .AsNoTracking()
                .Where(sm => sm.StartupId == domainEvent.StartupId
                          && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration))
                .Select(sm => sm.ProfileId)
                .ToListAsync(cancellationToken);

            // The investor and the startup side are told in one transaction — both sides of a
            // completed deal should become visible together.
            List<Notification> notifications =
            [
                Notification.Create(
                    userId: domainEvent.InvestorProfileId,
                    type: NotificationType.InvestmentDealCompleted,
                    title: "Investment deal completed",
                    body: "Your investment deal has been completed.",
                    createdAt: utcNow,
                    referenceId: domainEvent.DealId),

                .. recipientIds.Select(recipientId => Notification.Create(
                    userId: recipientId,
                    type: NotificationType.InvestmentDealCompleted,
                    title: "Investment deal completed",
                    body: "An investment deal has been completed.",
                    createdAt: utcNow,
                    referenceId: domainEvent.DealId))
            ];

            await notificationService.PublishManyAsync(notifications, cancellationToken);
        }
    }
}
