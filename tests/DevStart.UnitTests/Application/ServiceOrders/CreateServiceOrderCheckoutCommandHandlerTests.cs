using DevStart.Application.ServiceOrders;
using DevStart.Application.ServiceOrders.Checkout;
using DevStart.Application.Subscriptions;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.Payments;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.ServiceOrders;

public sealed class CreateServiceOrderCheckoutCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private const decimal Price = 490m;
    private const decimal PromotionPrice = 1490m;

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FakePaymentProvider _provider = new();
    private readonly FakeNpdIncomeService _income = new();
    private Guid _userId;
    private Guid _startupId;

    /// <param name="withTermSheet">
    /// Leave false to keep TermSheet unpriced, which is how the "no catalog entry" path is exercised.
    /// </param>
    private CreateServiceOrderCheckoutCommandHandler CreateSut(bool withTermSheet = false)
    {
        List<ServiceCatalogItem> items =
        [
            new ServiceCatalogItem
            {
                ServiceType = ServiceType.ScoringReport,
                Price = Price,
                Currency = "RUB",
                Description = "Scoring report",
                AccessDays = 30,
            },
            new ServiceCatalogItem
            {
                ServiceType = ServiceType.Promotion,
                Price = PromotionPrice,
                Currency = "RUB",
                Description = "Promotion",
                AccessDays = 30,
            },
        ];
        if (withTermSheet)
        {
            items.Add(new ServiceCatalogItem
            {
                ServiceType = ServiceType.TermSheet,
                Price = 990m,
                Currency = "RUB",
                Description = "Term sheet",
                AccessDays = 0,
            });
        }

        var catalog = Options.Create(new ServiceCatalogOptions { Items = items });
        var checkout = Options.Create(new CheckoutOptions { ReturnUrl = "https://example.com/return" });

        return new CreateServiceOrderCheckoutCommandHandler(
            _db,
            new TestUserContext(_userId),
            new FixedDateTimeProvider { UtcNow = Now },
            _provider,
            _income,
            catalog,
            checkout,
            NullLogger<CreateServiceOrderCheckoutCommandHandler>.Instance);
    }

    private async Task SeedAsync(string? email = "buyer@example.com")
    {
        User user = User.Create("buyer", email ?? "buyer@example.com", "hash", Now);
        _userId = user.Id;
        if (email is null)
        {
            user.Email = string.Empty;
        }

        var startup = new Startup
        {
            Id = Guid.NewGuid(),
            Name = "Acme",
            PublicEmail = "acme@example.com",
            Stage = StartupStage.Seed,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        _startupId = startup.Id;

        _db.Users.Add(user);
        _db.Startups.Add(startup);
        await _db.SaveChangesAsync();
    }

    private async Task MakeFounderAsync()
    {
        _db.StartupMembers.Add(
            StartupMember.Create(_userId, _startupId, StartupRole.Founder, isPublic: true, Now));
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Checkout_creates_order_and_service_order_payment_and_returns_confirmation_url()
    {
        await SeedAsync();

        Result<ServiceOrderCheckoutResponse> result = await CreateSut().Handle(
            new CreateServiceOrderCheckoutCommand(ServiceType.ScoringReport, _startupId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ConfirmationUrl.ShouldBe(_provider.CreatedToReturn.ConfirmationUrl);

        ServiceOrder order = _db.ServiceOrders.Single();
        order.Status.ShouldBe(ServiceOrderStatus.Pending);
        order.Amount.ShouldBe(Price);
        order.TargetId.ShouldBe(_startupId);
        // Access is only granted at fulfillment, so a pending order carries no window yet.
        order.ExpiresAt.ShouldBeNull();

        Payment payment = _db.Payments.Single();
        payment.Purpose.ShouldBe(PaymentPurpose.ServiceOrder);
        payment.SubscriptionId.ShouldBeNull();
        payment.ServiceOrderId.ShouldBe(order.Id);
        payment.Amount.ShouldBe(Price);
        _provider.LastCreateInput!.ServiceOrderId.ShouldBe(order.Id);
    }

    [Fact]
    public async Task Checkout_unknown_service_type_fails_without_creating_anything()
    {
        await SeedAsync();

        Result<ServiceOrderCheckoutResponse> result = await CreateSut().Handle(
            new CreateServiceOrderCheckoutCommand(ServiceType.TermSheet, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ServiceOrders.UnknownServiceType");
        _db.ServiceOrders.Any().ShouldBeFalse();
        _db.Payments.Any().ShouldBeFalse();
    }

    [Fact]
    public async Task Checkout_blocked_by_income_limit_creates_nothing()
    {
        await SeedAsync();
        _income.ShouldBlock = true;

        Result<ServiceOrderCheckoutResponse> result = await CreateSut().Handle(
            new CreateServiceOrderCheckoutCommand(ServiceType.ScoringReport, _startupId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PaymentErrors.IncomeLimitReached);
        _db.ServiceOrders.Any().ShouldBeFalse();
        _db.Payments.Any().ShouldBeFalse();
        _provider.LastCreateInput.ShouldBeNull();
    }

    [Fact]
    public async Task Checkout_without_customer_email_fails()
    {
        await SeedAsync(email: null);

        Result<ServiceOrderCheckoutResponse> result = await CreateSut().Handle(
            new CreateServiceOrderCheckoutCommand(ServiceType.ScoringReport, _startupId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PaymentErrors.CustomerEmailMissing);
    }

    [Fact]
    public async Task Checkout_without_target_fails_before_touching_the_income_counter()
    {
        await SeedAsync();

        Result<ServiceOrderCheckoutResponse> result = await CreateSut().Handle(
            new CreateServiceOrderCheckoutCommand(ServiceType.ScoringReport, null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ServiceOrderErrors.TargetRequired);
        _db.ServiceOrders.Any().ShouldBeFalse();
        _provider.LastCreateInput.ShouldBeNull();
    }

    [Fact]
    public async Task Checkout_for_unknown_startup_fails()
    {
        await SeedAsync();

        Result<ServiceOrderCheckoutResponse> result = await CreateSut().Handle(
            new CreateServiceOrderCheckoutCommand(ServiceType.ScoringReport, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ServiceOrders.TargetNotFound");
        _db.ServiceOrders.Any().ShouldBeFalse();
    }

    [Fact]
    public async Task Checkout_for_banned_startup_fails()
    {
        await SeedAsync();
        Startup startup = _db.Startups.Single(s => s.Id == _startupId);
        startup.Ban("spam", null, Guid.NewGuid(), Now);
        await _db.SaveChangesAsync();

        Result<ServiceOrderCheckoutResponse> result = await CreateSut().Handle(
            new CreateServiceOrderCheckoutCommand(ServiceType.ScoringReport, _startupId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ServiceOrders.TargetNotFound");
    }

    [Fact]
    public async Task Checkout_when_access_is_already_held_fails_instead_of_charging_twice()
    {
        await SeedAsync();
        ServiceOrder existing = ServiceOrder.CreatePending(
            _userId, ServiceType.ScoringReport, _startupId, Price, "RUB", Now.AddDays(-1));
        existing.MarkPaid(Now.AddDays(-1));
        existing.MarkFulfilled(Now.AddDays(-1), accessDays: 30);
        _db.ServiceOrders.Add(existing);
        await _db.SaveChangesAsync();

        Result<ServiceOrderCheckoutResponse> result = await CreateSut().Handle(
            new CreateServiceOrderCheckoutCommand(ServiceType.ScoringReport, _startupId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ServiceOrderErrors.AlreadyOwned);
        _db.ServiceOrders.Count().ShouldBe(1);
    }

    [Fact]
    public async Task Checkout_after_access_expired_is_allowed_again()
    {
        await SeedAsync();
        ServiceOrder expired = ServiceOrder.CreatePending(
            _userId, ServiceType.ScoringReport, _startupId, Price, "RUB", Now.AddDays(-40));
        expired.MarkPaid(Now.AddDays(-40));
        expired.MarkFulfilled(Now.AddDays(-40), accessDays: 30);
        _db.ServiceOrders.Add(expired);
        await _db.SaveChangesAsync();

        Result<ServiceOrderCheckoutResponse> result = await CreateSut().Handle(
            new CreateServiceOrderCheckoutCommand(ServiceType.ScoringReport, _startupId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _db.ServiceOrders.Count().ShouldBe(2);
    }

    [Fact]
    public async Task Promotion_by_a_non_member_is_rejected()
    {
        await SeedAsync();

        Result<ServiceOrderCheckoutResponse> result = await CreateSut().Handle(
            new CreateServiceOrderCheckoutCommand(ServiceType.Promotion, _startupId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ServiceOrderErrors.TargetNotAllowed);
        _db.ServiceOrders.Any().ShouldBeFalse();
    }

    [Fact]
    public async Task Promotion_by_a_founder_is_allowed_and_can_be_topped_up()
    {
        await SeedAsync();
        await MakeFounderAsync();

        ServiceOrder running = ServiceOrder.CreatePending(
            _userId, ServiceType.Promotion, _startupId, PromotionPrice, "RUB", Now.AddDays(-1));
        running.MarkPaid(Now.AddDays(-1));
        running.MarkFulfilled(Now.AddDays(-1), accessDays: 30);
        _db.ServiceOrders.Add(running);
        await _db.SaveChangesAsync();

        // Unlike the other services, buying promotion again buys more days — it is not "already owned".
        Result<ServiceOrderCheckoutResponse> result = await CreateSut().Handle(
            new CreateServiceOrderCheckoutCommand(ServiceType.Promotion, _startupId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _db.ServiceOrders.Count().ShouldBe(2);
    }

    private async Task<Guid> SeedDealAsync(Guid investorProfileId)
    {
        var deal = new InvestmentDeal
        {
            Id = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            StartupId = _startupId,
            InvestorProfileId = investorProfileId,
            Amount = 1_000_000m,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        _db.InvestmentDeals.Add(deal);
        await _db.SaveChangesAsync();
        return deal.Id;
    }

    [Fact]
    public async Task TermSheet_for_someone_elses_deal_is_rejected()
    {
        await SeedAsync();
        Guid dealId = await SeedDealAsync(investorProfileId: Guid.NewGuid());

        Result<ServiceOrderCheckoutResponse> result = await CreateSut(withTermSheet: true).Handle(
            new CreateServiceOrderCheckoutCommand(ServiceType.TermSheet, dealId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ServiceOrderErrors.TargetNotAllowed);
        _db.ServiceOrders.Any().ShouldBeFalse();
    }

    [Fact]
    public async Task TermSheet_for_the_callers_own_deal_is_allowed()
    {
        await SeedAsync();
        Guid dealId = await SeedDealAsync(investorProfileId: _userId);

        Result<ServiceOrderCheckoutResponse> result = await CreateSut(withTermSheet: true).Handle(
            new CreateServiceOrderCheckoutCommand(ServiceType.TermSheet, dealId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _db.ServiceOrders.Single().TargetId.ShouldBe(dealId);
    }
}
