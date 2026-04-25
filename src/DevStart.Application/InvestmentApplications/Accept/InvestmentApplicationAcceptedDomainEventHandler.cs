using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.Notifications;
using DevStart.SharedKernel;

namespace DevStart.Application.InvestmentApplications.Accept
{
    internal sealed class InvestmentApplicationAcceptedDomainEventHandler(
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<InvestmentApplicationAcceptedDomainEvent>
    {
        public async Task Handle(InvestmentApplicationAcceptedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            Notification notification = Notification.Create(
                userId: domainEvent.InvestorProfileId,
                type: NotificationType.InvestmentApplicationAccepted,
                title: "Investment application accepted",
                body: "Your investment application has been accepted. A deal has been created.",
                createdAt: dateTimeProvider.UtcNow,
                referenceId: domainEvent.DealId);

            await notificationService.PublishAsync(notification, cancellationToken);
        }
    }
}
