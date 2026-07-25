using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Payments.Npd;
using DevStart.Domain.Notifications;
using DevStart.Domain.Payments;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace DevStart.Application.Payments.Succeeded
{
    /// <summary>
    /// SC-42: when a succeeded payment pushes the calendar-year НПД income across the 80% warning
    /// threshold, alert every platform admin once. The check compares the year income *before* this
    /// payment (<c>prior</c>) against <c>prior + amount</c>, so exactly the crossing payment fires the
    /// alert regardless of domain-event dispatch ordering.
    /// </summary>
    internal sealed class PaymentSucceededDomainEventHandler(
        IApplicationDbContext context,
        INpdIncomeService incomeService,
        INotificationService notificationService,
        IOptions<NpdOptions> npdOptions,
        IDateTimeProvider dateTimeProvider) : IDomainEventHandler<PaymentSucceededDomainEvent>
    {
        public async Task Handle(PaymentSucceededDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            NpdOptions options = npdOptions.Value;
            decimal warningAmount = options.WarningAmount;

            int year = incomeService.ResolveIncomeYear(domainEvent.PaidAt);
            decimal priorIncome = await incomeService.GetYearToDateIncomeAsync(
                year, domainEvent.PaymentId, cancellationToken);

            bool crossesWarning = priorIncome < warningAmount
                && priorIncome + domainEvent.Amount >= warningAmount;
            if (!crossesWarning)
            {
                return;
            }

            List<Guid> adminIds = await context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserSystemRole.Admin)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            decimal total = priorIncome + domainEvent.Amount;
            CultureInfo ru = CultureInfo.GetCultureInfo("ru-RU");
            string body = string.Format(
                ru,
                "Доход НПД за {0} год достиг {1:N0} ₽ — это 80% годового лимита ({2:N0} ₽ из {3:N0} ₽). " +
                "Приближается предел; новые платные операции будут заблокированы при достижении лимита.",
                year, total, warningAmount, options.AnnualIncomeLimit);

            foreach (Guid adminId in adminIds)
            {
                Notification notification = Notification.Create(
                    userId: adminId,
                    type: NotificationType.IncomeLimitWarning,
                    title: "НПД: доход достиг 80% годового лимита",
                    body: body,
                    createdAt: dateTimeProvider.UtcNow,
                    referenceId: domainEvent.PaymentId);

                await notificationService.PublishAsync(notification, cancellationToken);
            }
        }
    }
}
