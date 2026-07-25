using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.Notifications;
using DevStart.Domain.ServiceOrders;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ServiceOrders.Paid
{
    /// <summary>
    /// SC-49 fulfillment hook: when a one-time service order is paid, grant the entitlement (mark it
    /// fulfilled) and notify the buyer. Concrete per-<see cref="ServiceType"/> delivery — generating the
    /// scoring report, applying the promotion — is a separate feature that should hook in here.
    /// </summary>
    internal sealed class ServiceOrderPaidDomainEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<ServiceOrderPaidDomainEvent>
    {
        public async Task Handle(ServiceOrderPaidDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            ServiceOrder? order = await context.ServiceOrders
                .SingleOrDefaultAsync(o => o.Id == domainEvent.ServiceOrderId, cancellationToken);
            if (order is null)
            {
                return;
            }

            DateTime utcNow = dateTimeProvider.UtcNow;
            if (order.MarkFulfilled(utcNow).IsSuccess)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            Notification notification = Notification.Create(
                userId: domainEvent.UserId,
                type: NotificationType.ServiceOrderFulfilled,
                title: "Услуга оплачена",
                body: $"Оплата услуги «{ServiceTitle(domainEvent.ServiceType)}» получена. Услуга активирована.",
                createdAt: utcNow,
                referenceId: domainEvent.ServiceOrderId);
            await notificationService.PublishAsync(notification, cancellationToken);
        }

        private static string ServiceTitle(ServiceType serviceType) => serviceType switch
        {
            ServiceType.ScoringReport => "Скоринг-отчёт",
            ServiceType.TermSheet => "Генерация term sheet",
            ServiceType.Promotion => "Продвижение",
            _ => serviceType.ToString(),
        };
    }
}
