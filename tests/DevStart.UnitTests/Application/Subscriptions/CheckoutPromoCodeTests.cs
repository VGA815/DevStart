using DevStart.Application.Payments.Sync;
using DevStart.Application.Subscriptions;
using DevStart.Application.Subscriptions.Checkout;
using DevStart.Domain.Payments;
using DevStart.Domain.PromoCodes;
using DevStart.Domain.Subscriptions;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.Subscriptions;

public sealed class CheckoutPromoCodeTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private const decimal Price = 990m;

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FakePaymentProvider _provider = new();
    private readonly Guid _userId;
    private readonly CreateCheckoutCommandHandler _sut;

    public CheckoutPromoCodeTests()
    {
        var clock = new FixedDateTimeProvider { UtcNow = Now };
        var plans = Options.Create(new PlansOptions
        {
            Pro = new PlanOptions { Price = Price, Currency = "RUB", DurationDays = 30, Description = "Pro" },
        });
        var checkout = Options.Create(new CheckoutOptions { ReturnUrl = "https://example.com/return" });
        var sync = new SyncPaymentStatusCommandHandler(
            _db, _provider, clock, plans, NullLogger<SyncPaymentStatusCommandHandler>.Instance);

        User user = User.Create("buyer", "buyer@example.com", "hash", Now);
        _userId = user.Id;
        _db.Users.Add(user);
        _db.SaveChanges();

        _sut = new CreateCheckoutCommandHandler(
            _db, new TestUserContext(_userId), clock, _provider, new FakeNpdIncomeService(), plans, checkout, sync,
            NullLogger<CreateCheckoutCommandHandler>.Instance);
    }

    private async Task<PromoCode> SeedPromoAsync(
        PromoDiscountType type, decimal value, int? freeDays = null, int? maxRedemptions = null)
    {
        PromoCode promo = PromoCode.Create(
            "PROMO", type, value, freeDays, SubscriptionPlan.Pro, maxRedemptions, null, null, Guid.NewGuid(), Now);
        _db.PromoCodes.Add(promo);
        await _db.SaveChangesAsync();
        return promo;
    }

    [Fact]
    public async Task FreePeriodCode_ActivatesWithoutProvider_AndRecordsRedemption()
    {
        PromoCode promo = await SeedPromoAsync(PromoDiscountType.FreePeriod, 0m, freeDays: 14);

        Result<CheckoutResponse> result =
            await _sut.Handle(new CreateCheckoutCommand(SubscriptionPlan.Pro, "promo"), default);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Activated.ShouldBeTrue();
        result.Value.ConfirmationUrl.ShouldBeNull();
        _provider.LastCreateInput.ShouldBeNull();

        Subscription sub = await _db.Subscriptions.SingleAsync();
        sub.Status.ShouldBe(SubscriptionStatus.Active);
        sub.Source.ShouldBe(SubscriptionSource.Promo);
        sub.ExpiresAt.ShouldBe(Now.AddDays(14));

        (await _db.PromoCodeRedemptions.CountAsync(r => r.PromoCodeId == promo.Id && r.UserId == _userId)).ShouldBe(1);
        (await _db.PromoCodes.SingleAsync(p => p.Id == promo.Id)).RedeemedCount.ShouldBe(1);
        (await _db.Payments.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task PercentageCode_ChargesDiscountedAmount_AndDefersRedemption()
    {
        PromoCode promo = await SeedPromoAsync(PromoDiscountType.Percentage, 50m);

        Result<CheckoutResponse> result =
            await _sut.Handle(new CreateCheckoutCommand(SubscriptionPlan.Pro, "promo"), default);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Activated.ShouldBeFalse();
        result.Value.ConfirmationUrl.ShouldNotBeNull();

        _provider.LastCreateInput.ShouldNotBeNull();
        _provider.LastCreateInput!.Amount.ShouldBe(495m);

        Payment payment = await _db.Payments.SingleAsync();
        payment.Amount.ShouldBe(495m);
        payment.DiscountAmount.ShouldBe(495m);
        payment.PromoCodeId.ShouldBe(promo.Id);

        // Redemption is only finalized once the payment succeeds.
        (await _db.PromoCodeRedemptions.CountAsync()).ShouldBe(0);
        (await _db.PromoCodes.SingleAsync(p => p.Id == promo.Id)).RedeemedCount.ShouldBe(0);
    }

    [Fact]
    public async Task UnknownCode_ReturnsInvalidCode()
    {
        Result<CheckoutResponse> result =
            await _sut.Handle(new CreateCheckoutCommand(SubscriptionPlan.Pro, "nope"), default);

        result.Error.ShouldBe(PromoCodeErrors.InvalidCode);
    }

    [Fact]
    public async Task AlreadyRedeemedByUser_IsRejected()
    {
        PromoCode promo = await SeedPromoAsync(PromoDiscountType.Percentage, 50m);
        _db.PromoCodeRedemptions.Add(PromoCodeRedemption.Create(
            promo.Id, _userId, Guid.NewGuid(), null, 495m, Now));
        await _db.SaveChangesAsync();

        Result<CheckoutResponse> result =
            await _sut.Handle(new CreateCheckoutCommand(SubscriptionPlan.Pro, "promo"), default);

        result.Error.ShouldBe(PromoCodeErrors.AlreadyRedeemedByUser);
    }
}
