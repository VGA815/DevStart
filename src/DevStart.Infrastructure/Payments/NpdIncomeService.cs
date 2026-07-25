using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Payments.Npd;
using DevStart.Domain.Payments;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.Infrastructure.Payments
{
    /// <summary>
    /// Computes self-employed (НПД) net income per calendar year and enforces the annual cap.
    /// Net income = Σ(Amount − RefundedAmount) over Succeeded/Refunded payments whose <c>PaidAt</c>
    /// falls in the year, so a refund automatically lowers the counter. Year boundaries are drawn in
    /// the configured time zone (МСК has no DST, so conversion is unambiguous).
    /// </summary>
    internal sealed class NpdIncomeService(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        IOptions<NpdOptions> options) : INpdIncomeService
    {
        private readonly NpdOptions _options = options.Value;
        private readonly TimeZoneInfo _timeZone = ResolveTimeZone(options.Value.IncomeTimeZone);

        public int ResolveIncomeYear(DateTime momentUtc)
        {
            DateTime utc = momentUtc.Kind == DateTimeKind.Utc
                ? momentUtc
                : DateTime.SpecifyKind(momentUtc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, _timeZone).Year;
        }

        public async Task<decimal> GetYearToDateIncomeAsync(
            int year,
            Guid? excludePaymentId,
            CancellationToken cancellationToken)
        {
            (DateTime startUtc, DateTime endUtc) = YearWindowUtc(year);

            IQueryable<Payment> query = context.Payments
                .AsNoTracking()
                .Where(p => (p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.Refunded)
                         && p.PaidAt >= startUtc
                         && p.PaidAt < endUtc);

            if (excludePaymentId is Guid excludeId)
            {
                query = query.Where(p => p.Id != excludeId);
            }

            // SumAsync over an empty set returns 0 for a non-nullable decimal selector.
            return await query.SumAsync(p => p.Amount - p.RefundedAmount, cancellationToken);
        }

        public async Task<Result> EnsureCanAcceptPaymentAsync(
            decimal amount,
            CancellationToken cancellationToken)
        {
            int year = ResolveIncomeYear(dateTimeProvider.UtcNow);
            decimal yearToDate = await GetYearToDateIncomeAsync(year, null, cancellationToken);

            // Do not let a new payment *cross* the cap (reaching it exactly is allowed).
            return yearToDate + amount > _options.AnnualIncomeLimit
                ? Result.Failure(PaymentErrors.IncomeLimitReached)
                : Result.Success();
        }

        private (DateTime StartUtc, DateTime EndUtc) YearWindowUtc(int year)
        {
            var startLocal = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var endLocal = new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            DateTime startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, _timeZone);
            DateTime endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, _timeZone);
            return (startUtc, endUtc);
        }

        private static TimeZoneInfo ResolveTimeZone(string id)
        {
            foreach (string candidate in new[] { id, "Europe/Moscow", "Russian Standard Time" })
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(candidate);
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            // Moscow Standard Time is a fixed UTC+3 with no daylight saving.
            return TimeZoneInfo.CreateCustomTimeZone("MSK", TimeSpan.FromHours(3), "MSK", "MSK");
        }
    }
}
