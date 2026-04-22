using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.Notifications;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMembers.Create
{
    internal sealed class StartupMemberCreatedDomainEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<StartupMemberCreatedDomainEvent>
    {
        public async Task Handle(StartupMemberCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            Startup? startup = await context.Startups
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.Id == domainEvent.StartupId, cancellationToken);

            string startupName = startup?.Name ?? "a startup";

            Notification notification = Notification.Create(
                userId: domainEvent.ProfileId,
                type: NotificationType.StartupMemberAdded,
                title: "You joined a startup",
                body: $"You have been added to \"{startupName}\".",
                createdAt: dateTimeProvider.UtcNow,
                referenceId: domainEvent.StartupId);

            await notificationService.PublishAsync(notification, cancellationToken);
        }
    }
}
