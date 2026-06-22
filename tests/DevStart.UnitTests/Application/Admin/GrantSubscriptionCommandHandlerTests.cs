using DevStart.Application.Admin.Subscriptions.GrantSubscription;
using DevStart.Application.Subscriptions;
using DevStart.Domain.Admin;
using DevStart.Domain.Subscriptions;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.Admin;

public sealed class GrantSubscriptionCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FixedDateTimeProvider _clock = new() { UtcNow = Now };
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly GrantSubscriptionCommandHandler _sut;

    public GrantSubscriptionCommandHandlerTests()
    {
        var plans = Options.Create(new PlansOptions
        {
            Pro = new PlanOptions { Price = 990m, Currency = "RUB", DurationDays = 30, Description = "Pro" },
        });
        _sut = new GrantSubscriptionCommandHandler(_db, new TestUserContext(_adminId), plans, _clock);
    }

    private async Task<User> SeedUserAsync()
    {
        User user = User.Create("u", "u@example.com", "hash", Now);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Grant_CreatesActiveProSubscription_FromAdminGrant_WithDefaultDuration()
    {
        User user = await SeedUserAsync();

        Result<Guid> result = await _sut.Handle(new GrantSubscriptionCommand(user.Id, null, "loyalty"), default);

        result.IsSuccess.ShouldBeTrue();

        Subscription sub = await _db.Subscriptions.SingleAsync(s => s.Id == result.Value);
        sub.Status.ShouldBe(SubscriptionStatus.Active);
        sub.Plan.ShouldBe(SubscriptionPlan.Pro);
        sub.Source.ShouldBe(SubscriptionSource.AdminGrant);
        sub.ExpiresAt.ShouldBe(Now.AddDays(30));

        (await _db.AdminActionLogs.SingleAsync()).ActionType.ShouldBe(AdminActionType.GrantSubscription);
    }

    [Fact]
    public async Task Grant_UsesProvidedDuration()
    {
        User user = await SeedUserAsync();

        Result<Guid> result = await _sut.Handle(new GrantSubscriptionCommand(user.Id, 7, "trial"), default);

        Subscription sub = await _db.Subscriptions.SingleAsync(s => s.Id == result.Value);
        sub.ExpiresAt.ShouldBe(Now.AddDays(7));
    }

    [Fact]
    public async Task Grant_WhenAlreadyActive_ReturnsAlreadyActive()
    {
        User user = await SeedUserAsync();
        Subscription active = Subscription.CreatePending(user.Id, SubscriptionPlan.Pro, Now);
        active.Activate(Now, 30);
        _db.Subscriptions.Add(active);
        await _db.SaveChangesAsync();

        Result<Guid> result = await _sut.Handle(new GrantSubscriptionCommand(user.Id, null, "x"), default);

        result.Error.ShouldBe(SubscriptionErrors.AlreadyActive);
    }

    [Fact]
    public async Task Grant_UnknownUser_ReturnsNotFound()
    {
        Guid missing = Guid.NewGuid();

        Result<Guid> result = await _sut.Handle(new GrantSubscriptionCommand(missing, null, "x"), default);

        result.Error.ShouldBe(UserErrors.NotFound(missing));
    }
}
