using DevStart.Application.ServiceOrders;
using DevStart.Application.ServiceOrders.Checkout;
using DevStart.Application.Subscriptions;
using DevStart.Domain.Payments;
using DevStart.Domain.ServiceOrders;
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

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FakePaymentProvider _provider = new();
    private readonly FakeNpdIncomeService _income = new();
    private Guid _userId;

    private CreateServiceOrderCheckoutCommandHandler CreateSut()
    {
        var catalog = Options.Create(new ServiceCatalogOptions
        {
            Items =
            [
                new ServiceCatalogItem
                {
                    ServiceType = ServiceType.ScoringReport,
                    Price = Price,
                    Currency = "RUB",
                    Description = "Scoring report",
                },
            ],
        });
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

    private async Task SeedUserAsync(string? email = "buyer@example.com")
    {
        User user = User.Create("buyer", email ?? "buyer@example.com", "hash", Now);
        _userId = user.Id;
        if (email is null)
        {
            user.Email = string.Empty;
        }
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Checkout_creates_order_and_service_order_payment_and_returns_confirmation_url()
    {
        await SeedUserAsync();

        Result<ServiceOrderCheckoutResponse> result = await CreateSut()
            .Handle(new CreateServiceOrderCheckoutCommand(ServiceType.ScoringReport), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ConfirmationUrl.ShouldBe(_provider.CreatedToReturn.ConfirmationUrl);

        ServiceOrder order = _db.ServiceOrders.Single();
        order.Status.ShouldBe(ServiceOrderStatus.Pending);
        order.Amount.ShouldBe(Price);

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
        await SeedUserAsync();

        Result<ServiceOrderCheckoutResponse> result = await CreateSut()
            .Handle(new CreateServiceOrderCheckoutCommand(ServiceType.Promotion), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ServiceOrders.UnknownServiceType");
        _db.ServiceOrders.Any().ShouldBeFalse();
        _db.Payments.Any().ShouldBeFalse();
    }

    [Fact]
    public async Task Checkout_blocked_by_income_limit_creates_nothing()
    {
        await SeedUserAsync();
        _income.ShouldBlock = true;

        Result<ServiceOrderCheckoutResponse> result = await CreateSut()
            .Handle(new CreateServiceOrderCheckoutCommand(ServiceType.ScoringReport), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PaymentErrors.IncomeLimitReached);
        _db.ServiceOrders.Any().ShouldBeFalse();
        _db.Payments.Any().ShouldBeFalse();
        _provider.LastCreateInput.ShouldBeNull();
    }

    [Fact]
    public async Task Checkout_without_customer_email_fails()
    {
        await SeedUserAsync(email: null);

        Result<ServiceOrderCheckoutResponse> result = await CreateSut()
            .Handle(new CreateServiceOrderCheckoutCommand(ServiceType.ScoringReport), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PaymentErrors.CustomerEmailMissing);
    }
}
