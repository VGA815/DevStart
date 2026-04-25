using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.Notifications;
using DevStart.SharedKernel;

namespace DevStart.Application.InvestmentApplications.Reject
{
    internal sealed class InvestmentApplicationRejectedDomainEventHandler(
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<InvestmentApplicationRejectedDomainEvent>
    {
        public async Task Handle(InvestmentApplicationRejectedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            Notification notification = Notification.Create(
                userId: domainEvent.InvestorProfileId,
                type: NotificationType.InvestmentApplicationRejected,
                title: "Investment application rejected",
                body: "Your investment application has been rejected.",
                createdAt: dateTimeProvider.UtcNow,
                referenceId: domainEvent.ApplicationId);

            await notificationService.PublishAsync(notification, cancellationToken);
        }
    }
}
