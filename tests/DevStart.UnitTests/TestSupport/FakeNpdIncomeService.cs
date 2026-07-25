using DevStart.Application.Abstractions.Payments;
using DevStart.Domain.Payments;
using DevStart.SharedKernel;

namespace DevStart.UnitTests.TestSupport
{
    /// <summary>
    /// Test double for <see cref="INpdIncomeService"/>. Allows all payments by default; set
    /// <see cref="ShouldBlock"/> to simulate the НПД annual income limit being reached.
    /// </summary>
    internal sealed class FakeNpdIncomeService : INpdIncomeService
    {
        public bool ShouldBlock { get; set; }
        public decimal YearToDateIncome { get; set; }

        public int ResolveIncomeYear(DateTime momentUtc) => momentUtc.Year;

        public Task<decimal> GetYearToDateIncomeAsync(
            int year, Guid? excludePaymentId, CancellationToken cancellationToken)
            => Task.FromResult(YearToDateIncome);

        public Task<Result> EnsureCanAcceptPaymentAsync(decimal amount, CancellationToken cancellationToken)
            => Task.FromResult(ShouldBlock
                ? Result.Failure(PaymentErrors.IncomeLimitReached)
                : Result.Success());
    }
}
