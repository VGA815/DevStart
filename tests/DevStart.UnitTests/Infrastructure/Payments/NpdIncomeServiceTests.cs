using DevStart.Application.Payments.Npd;
using DevStart.Domain.Payments;
using DevStart.Infrastructure.Database;
using DevStart.Infrastructure.Payments;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Infrastructure.Payments;

public sealed class NpdIncomeServiceTests
{
    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();

    private static readonly NpdOptions Options = new()
    {
        AnnualIncomeLimit = 2_400_000m,
        WarningThresholdFraction = 0.80m,
        IncomeTimeZone = "Europe/Moscow",
    };

    private NpdIncomeService CreateSut(DateTime nowUtc) =>
        new(_db, new FixedDateTimeProvider { UtcNow = nowUtc }, Microsoft.Extensions.Options.Options.Create(Options));

    private Payment AddPayment(decimal amount, decimal refunded, PaymentStatus status, DateTime paidAtUtc)
    {
        Payment payment = Payment.CreatePending(
            Guid.NewGuid(), Guid.NewGuid(), PaymentProvider.YooKassa, amount, "RUB", paidAtUtc);
        payment.Status = status;
        payment.RefundedAmount = refunded;
        payment.PaidAt = paidAtUtc;
        _db.Payments.Add(payment);
        return payment;
    }

    [Fact]
    public async Task Income_is_net_of_refunds_over_succeeded_and_refunded_payments_only()
    {
        AddPayment(1000m, 0m, PaymentStatus.Succeeded, new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc));
        AddPayment(500m, 200m, PaymentStatus.Succeeded, new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc)); // net 300
        AddPayment(800m, 800m, PaymentStatus.Refunded, new DateTime(2026, 4, 5, 9, 0, 0, DateTimeKind.Utc));  // net 0
        AddPayment(999m, 0m, PaymentStatus.Pending, new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc));     // excluded
        AddPayment(777m, 0m, PaymentStatus.Cancelled, new DateTime(2026, 4, 3, 9, 0, 0, DateTimeKind.Utc));   // excluded
        await _db.SaveChangesAsync();

        NpdIncomeService sut = CreateSut(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        (await sut.GetYearToDateIncomeAsync(2026, null, CancellationToken.None)).ShouldBe(1300m);
    }

    [Fact]
    public async Task Income_excludes_the_given_payment_id()
    {
        Payment a = AddPayment(1000m, 0m, PaymentStatus.Succeeded, new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc));
        AddPayment(400m, 0m, PaymentStatus.Succeeded, new DateTime(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc));
        await _db.SaveChangesAsync();

        NpdIncomeService sut = CreateSut(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        (await sut.GetYearToDateIncomeAsync(2026, a.Id, CancellationToken.None)).ShouldBe(400m);
    }

    [Fact]
    public async Task Year_boundary_is_evaluated_in_Moscow_time()
    {
        // 2026-12-31 21:00 UTC == 2027-01-01 00:00 МСК → belongs to 2027.
        AddPayment(1000m, 0m, PaymentStatus.Succeeded, new DateTime(2026, 12, 31, 21, 0, 0, DateTimeKind.Utc));
        // 2026-12-31 20:59 UTC == 2026-12-31 23:59 МСК → belongs to 2026.
        AddPayment(500m, 0m, PaymentStatus.Succeeded, new DateTime(2026, 12, 31, 20, 59, 0, DateTimeKind.Utc));
        await _db.SaveChangesAsync();

        NpdIncomeService sut = CreateSut(new DateTime(2027, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        (await sut.GetYearToDateIncomeAsync(2026, null, CancellationToken.None)).ShouldBe(500m);
        (await sut.GetYearToDateIncomeAsync(2027, null, CancellationToken.None)).ShouldBe(1000m);
    }

    [Fact]
    public async Task EnsureCanAccept_blocks_a_payment_that_would_cross_the_limit()
    {
        AddPayment(2_399_500m, 0m, PaymentStatus.Succeeded, new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc));
        await _db.SaveChangesAsync();
        NpdIncomeService sut = CreateSut(new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc));

        // 2_399_500 + 990 = 2_400_490 > 2_400_000 → blocked.
        Result result = await sut.EnsureCanAcceptPaymentAsync(990m, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PaymentErrors.IncomeLimitReached);
    }

    [Fact]
    public async Task EnsureCanAccept_allows_reaching_the_limit_exactly()
    {
        AddPayment(2_399_010m, 0m, PaymentStatus.Succeeded, new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc));
        await _db.SaveChangesAsync();
        NpdIncomeService sut = CreateSut(new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc));

        // 2_399_010 + 990 = 2_400_000 == limit → allowed.
        (await sut.EnsureCanAcceptPaymentAsync(990m, CancellationToken.None)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task EnsureCanAccept_allows_again_after_a_refund_frees_headroom()
    {
        // 2_400_000 charged, 1000 refunded → net 2_399_000; +990 = 2_399_990 ≤ limit → allowed.
        AddPayment(2_400_000m, 1000m, PaymentStatus.Succeeded, new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc));
        await _db.SaveChangesAsync();
        NpdIncomeService sut = CreateSut(new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc));

        (await sut.EnsureCanAcceptPaymentAsync(990m, CancellationToken.None)).IsSuccess.ShouldBeTrue();
    }
}
