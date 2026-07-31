using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Application.Abstractions.ServiceOrders;
using DevStart.Domain.Notifications;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace DevStart.Application.ServiceOrders.Paid
{
    /// <summary>
    /// SC-49 fulfillment: when a one-time service order is paid, actually deliver it.
    /// <para>
    /// Scoring reports and term sheets are delivered as an entitlement — the fulfilled order itself is
    /// what the Pro gates read, so nothing further has to be written. Promotion is delivered by
    /// featuring the startup until the paid window ends.
    /// </para>
    /// </summary>
    internal sealed class ServiceOrderPaidDomainEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IServiceEntitlementChecker entitlementChecker,
        ICacheService cacheService,
        IOptions<ServiceCatalogOptions> catalogOptions,
        IDateTimeProvider dateTimeProvider,
        ILogger<ServiceOrderPaidDomainEventHandler> logger) : IDomainEventHandler<ServiceOrderPaidDomainEvent>
    {
        public async Task Handle(ServiceOrderPaidDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            ServiceOrder? order = await context.ServiceOrders
                .SingleOrDefaultAsync(o => o.Id == domainEvent.ServiceOrderId, cancellationToken);
            if (order is null)
            {
                return;
            }

            // Only a genuine Paid → Fulfilled transition delivers. MarkFulfilled is idempotent and
            // reports success when the order is already fulfilled, so testing its result would let a
            // replayed webhook deliver twice — extending a promotion that was paid for once.
            if (order.Status != ServiceOrderStatus.Paid)
            {
                return;
            }

            DateTime utcNow = dateTimeProvider.UtcNow;
            int accessDays = catalogOptions.Value.Find(domainEvent.ServiceType)?.AccessDays ?? 0;

            if (order.MarkFulfilled(utcNow, accessDays).IsFailure)
            {
                return;
            }

            string? targetName = await DeliverAsync(order, accessDays, utcNow, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            // The gates read the entitlement through a cached checker; a stale "no access" answer would
            // hold back the thing that was just paid for.
            await entitlementChecker.InvalidateAsync(order.UserId, cancellationToken);

            Notification notification = Notification.Create(
                userId: domainEvent.UserId,
                type: NotificationType.ServiceOrderFulfilled,
                title: "Услуга оплачена",
                body: BuildBody(domainEvent.ServiceType, targetName, order.ExpiresAt),
                createdAt: utcNow,
                referenceId: domainEvent.ServiceOrderId);
            await notificationService.PublishAsync(notification, cancellationToken);
        }

        /// <summary>Performs the per-service delivery. Returns the target's display name, when it has one.</summary>
        private async Task<string?> DeliverAsync(
            ServiceOrder order,
            int accessDays,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            switch (order.ServiceType)
            {
                case ServiceType.Promotion:
                {
                    Startup? startup = await context.Startups
                        .SingleOrDefaultAsync(s => s.Id == order.TargetId, cancellationToken);
                    if (startup is null)
                    {
                        // The startup was removed between checkout and capture. The money is in and the
                        // order stays fulfilled; an admin resolves it from the service-orders page.
                        logger.LogError(
                            "Paid promotion order {ServiceOrderId} targets startup {StartupId}, which no longer exists.",
                            order.Id, order.TargetId);
                        return null;
                    }

                    startup.Feature(accessDays, utcNow);
                    await cacheService.RemoveAsync(CacheKeys.Startup(startup.Id), cancellationToken);
                    return startup.Name;
                }

                case ServiceType.ScoringReport:
                {
                    // Delivered as an entitlement: the fulfilled order is what GetStartupScoreQueryHandler reads.
                    return await context.Startups
                        .AsNoTracking()
                        .Where(s => s.Id == order.TargetId)
                        .Select(s => s.Name)
                        .SingleOrDefaultAsync(cancellationToken);
                }

                default:
                    // TermSheet is delivered as an entitlement too, and a deal has no display name.
                    return null;
            }
        }

        private static string BuildBody(ServiceType serviceType, string? targetName, DateTime? expiresAt)
        {
            string subject = targetName is null
                ? $"«{ServiceTitle(serviceType)}»"
                : $"«{ServiceTitle(serviceType)}» для «{targetName}»";
            string until = expiresAt is null
                ? "Доступ открыт бессрочно."
                : $"Доступ открыт до {expiresAt.Value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("ru-RU"))}.";

            return $"Оплата услуги {subject} получена. {until}";
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
