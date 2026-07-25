using DevStart.SharedKernel;

namespace DevStart.Application.Abstractions.Payments
{
    /// <summary>
    /// Tracks the self-employed (НПД) cumulative income against the annual legal cap (ФЗ-422).
    /// Income = confirmed payments minus refunds (net), attributed to the payment's calendar year.
    /// </summary>
    public interface INpdIncomeService
    {
        /// <summary>Calendar year (in the configured НПД time zone) that a UTC instant falls into.</summary>
        int ResolveIncomeYear(DateTime momentUtc);

        /// <summary>
        /// Net income (Σ Amount − RefundedAmount over succeeded/refunded payments) for the given
        /// calendar year. <paramref name="excludePaymentId"/> omits one payment (used when checking
        /// the "before this payment" income so the result is independent of event-dispatch ordering).
        /// </summary>
        Task<decimal> GetYearToDateIncomeAsync(int year, Guid? excludePaymentId, CancellationToken cancellationToken);

        /// <summary>
        /// Fails with <c>PaymentErrors.IncomeLimitReached</c> when accepting a new payment of
        /// <paramref name="amount"/> would push the current calendar year's net income over the limit.
        /// </summary>
        Task<Result> EnsureCanAcceptPaymentAsync(decimal amount, CancellationToken cancellationToken);
    }
}
