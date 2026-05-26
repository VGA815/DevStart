using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Payments.Sync;
using DevStart.Application.Subscriptions;
using DevStart.Application.Subscriptions.Checkout;
using DevStart.Domain.Payments;
using DevStart.Domain.Subscriptions;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.Subscriptions;

public sealed class CreateCheckoutCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 26, 12, 0, 0, DateTimeKind.Utc);
    private const decimal Amount = 990m;

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FakePaymentProvider _provider = new();
    private readonly Guid _userId;
    private readonly CreateCheckoutCommandHandler _sut;

    public CreateCheckoutCommandHandlerTests()
    {
        var clock = new FixedDateTimeProvider { UtcNow = Now };
        var plans = Options.Create(new PlansOptions
        {
            Pro = new PlanOptions { Price = Amount, Currency = "RUB", DurationDays = 30, Description = "DevStart Pro" },
        });
        var checkout = Options.Create(new CheckoutOptions { ReturnUrl = "https://example.com/return" });
        var sync = new SyncPaymentStatusCommandHandler(
            _db, _provider, clock, plans, NullLogger<SyncPaymentStatusCommandHandler>.Instance);

        User user = User.Create("buyer", "buyer@example.com", "hash", Now);
        _userId = user.Id;
        _db.Users.Add(user);
        _db.SaveChanges();

        _sut = new CreateCheckoutCommandHandler(
            _db, new TestUserContext(_userId), clock, _provider, plans, checkout, sync);
    }

    private async Task<(Subscription, Payment)> SeedPendingAsync(DateTime createdAt, string providerId, string url)
    {
        Subscription subscription = Subscription.CreatePending(_userId, SubscriptionPlan.Pro, createdAt);
        Payment payment = Payment.CreatePending(_userId, subscription.Id, PaymentProvider.YooKassa, Amount, "RUB", createdAt);
        payment.AssignProviderPayment(providerId, url);
        _db.Subscriptions.Add(subscription);
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return (subscription, payment);
    }

    [Fact]
    public async Task FreshPending_ReusesExistingLink_WithoutCreatingAnotherProviderPayment()
    {
        (_, Payment existing) = await SeedPendingAsync(Now, "pay-1", "https://pay/existing");

        Result<CheckoutResponse> result = await _sut.Handle(new CreateCheckoutCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ConfirmationUrl.ShouldBe("https://pay/existing");
        result.Value.PaymentId.ShouldBe(existing.Id);
        _provider.LastCreateInput.ShouldBeNull();
        _db.Payments.Count().ShouldBe(1);
    }

    [Fact]
    public async Task StalePending_ResolvedAsCancelled_CreatesFreshProviderPayment()
    {
        await SeedPendingAsync(Now.AddMinutes(-31), "pay-old", "https://pay/old");
        _provider.SnapshotToReturn = new ProviderPaymentSnapshot(
            "pay-old", PaymentStatus.Cancelled, Paid: false, null, RefundedAmount: 0m, null);
        _provider.CreatedToReturn = new CreatedPayment("pay-new", "https://pay/new");

        Result<CheckoutResponse> result = await _sut.Handle(new CreateCheckoutCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ConfirmationUrl.ShouldBe("https://pay/new");
        _provider.LastCreateInput.ShouldNotBeNull();
        _db.Payments.Count(p => p.Status == PaymentStatus.Cancelled).ShouldBe(1);
        _db.Payments.Count(p => p.Status == PaymentStatus.Pending).ShouldBe(1);
    }

    [Fact]
    public async Task StalePending_ResolvedAsSucceeded_ReturnsAlreadyActive()
    {
        await SeedPendingAsync(Now.AddMinutes(-31), "pay-old", "https://pay/old");
        _provider.SnapshotToReturn = new ProviderPaymentSnapshot(
            "pay-old", PaymentStatus.Succeeded, Paid: true, Now, RefundedAmount: 0m, "succeeded");

        Result<CheckoutResponse> result = await _sut.Handle(new CreateCheckoutCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SubscriptionErrors.AlreadyActive);
        _provider.LastCreateInput.ShouldBeNull();
    }
}
