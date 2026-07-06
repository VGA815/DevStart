using DevStart.Application.StartupEquity;
using DevStart.Application.StartupEquity.GetCapTable;
using DevStart.Application.StartupEquity.Vesting;
using DevStart.Application.Startups;
using DevStart.Domain.StartupEquity;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.StartupEquity;

public sealed class GetStartupCapTableQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 26, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FixedDateTimeProvider _clock = new();
    private readonly Guid _startupId = Guid.NewGuid();
    private readonly Guid _founderAId = Guid.NewGuid();
    private readonly Guid _founderBId = Guid.NewGuid();

    public GetStartupCapTableQueryHandlerTests()
    {
        _db.Startups.Add(new Startup
        {
            Id = _startupId,
            Name = "Acme",
            PublicEmail = "acme@example.com",
            Stage = StartupStage.Seed,
            CreatedAt = Now,
            UpdatedAt = Now,
        });
        _db.StartupMembers.Add(StartupMember.Create(_founderAId, _startupId, StartupRole.Founder, isPublic: true, Now));
        _db.StartupMembers.Add(StartupMember.Create(_founderBId, _startupId, StartupRole.Founder, isPublic: true, Now));
        _db.SaveChanges();
    }

    private GetStartupCapTableQueryHandler CreateSut(Guid callerId) =>
        new(_db,
            new TestUserContext(callerId),
            new StartupAuthorizationService(_db),
            new FoundingCapTableProvider(_db),
            new VestingCalculator(),
            _clock);

    [Fact]
    public async Task NoExplicitTable_ReturnsBootstrappedDefault()
    {
        GetStartupCapTableQueryHandler sut = CreateSut(_founderAId);

        Result<StartupCapTableResponse> result =
            await sut.Handle(new GetStartupCapTableQuery(_startupId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        StartupCapTableResponse table = result.Value;
        table.IsConfigured.ShouldBeFalse();
        table.Holders.Count.ShouldBe(3); // two founders + ESOP
        table.TotalPercentage.ShouldBe(100m);
        table.Holders.Count(h => h.HolderType == EquityHolderType.Founder).ShouldBe(2);
        table.Holders.Single(h => h.HolderType == EquityHolderType.Esop).EquityPercentage.ShouldBe(10m);
        // No schedules ⇒ fully vested.
        table.TotalVestedPercentage.ShouldBe(100m);
    }

    [Fact]
    public async Task ExplicitTable_ReportsConfigured_AndComputesVestedShare()
    {
        // Founder A: 60% vesting 48m/12m cliff started 24m before "now" ⇒ 50% vested ⇒ 30% vested share.
        _db.StartupEquityHolders.Add(StartupEquityHolder.Create(
            _startupId, EquityHolderType.Founder, _founderAId, null, 60m,
            _clock.UtcNow.AddMonths(-24), 48, 12, Now));
        _db.StartupEquityHolders.Add(StartupEquityHolder.Create(
            _startupId, EquityHolderType.Founder, _founderBId, null, 30m, null, null, null, Now));
        _db.StartupEquityHolders.Add(StartupEquityHolder.Create(
            _startupId, EquityHolderType.Esop, null, "ESOP pool", 10m, null, null, null, Now));
        await _db.SaveChangesAsync();

        GetStartupCapTableQueryHandler sut = CreateSut(_founderAId);

        Result<StartupCapTableResponse> result =
            await sut.Handle(new GetStartupCapTableQuery(_startupId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        StartupCapTableResponse table = result.Value;
        table.IsConfigured.ShouldBeTrue();
        table.Holders.Count.ShouldBe(3);

        StartupCapTableHolderResponse founderA = table.Holders.Single(h => h.ProfileId == _founderAId);
        founderA.VestedFraction.ShouldBe(0.5m);
        founderA.VestedPercentage.ShouldBe(30m);

        // Founder B and ESOP have no schedule ⇒ fully vested.
        table.Holders.Single(h => h.ProfileId == _founderBId).VestedPercentage.ShouldBe(30m);
        table.TotalVestedPercentage.ShouldBe(70m); // 30 + 30 + 10
    }

    [Fact]
    public async Task NonMember_IsUnauthorized()
    {
        GetStartupCapTableQueryHandler sut = CreateSut(Guid.NewGuid());

        Result<StartupCapTableResponse> result =
            await sut.Handle(new GetStartupCapTableQuery(_startupId), CancellationToken.None);

        result.Error.ShouldBe(StartupEquityErrors.Unauthorized);
    }
}
