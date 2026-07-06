using DevStart.Application.StartupEquity.SetCapTable;
using DevStart.Application.Startups;
using DevStart.Domain.StartupEquity;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.UnitTests.Application.StartupEquity;

public sealed class SetStartupCapTableCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 26, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FixedDateTimeProvider _clock = new();
    private readonly Guid _startupId = Guid.NewGuid();
    private readonly Guid _founderAId = Guid.NewGuid();
    private readonly Guid _founderBId = Guid.NewGuid();

    public SetStartupCapTableCommandHandlerTests()
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

    private SetStartupCapTableCommandHandler CreateSut(Guid callerId) =>
        new(_db, new TestUserContext(callerId), new StartupAuthorizationService(_db), _clock);

    private static CapTableHolderInput Founder(Guid id, decimal pct) =>
        new(EquityHolderType.Founder, id, null, pct, null, null, null);

    private static CapTableHolderInput Esop(decimal pct) =>
        new(EquityHolderType.Esop, null, "ESOP pool", pct, null, null, null);

    [Fact]
    public async Task ValidCapTable_ByFounder_Persists()
    {
        SetStartupCapTableCommandHandler sut = CreateSut(_founderAId);
        var command = new SetStartupCapTableCommand(_startupId,
            [Founder(_founderAId, 60m), Founder(_founderBId, 30m), Esop(10m)]);

        Result result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        List<StartupEquityHolder> rows = await _db.StartupEquityHolders
            .Where(h => h.StartupId == _startupId).ToListAsync();
        rows.Count.ShouldBe(3);
        rows.Single(r => r.ProfileId == _founderAId).EquityPercentage.ShouldBe(60m);
        rows.Single(r => r.ProfileId == _founderBId).EquityPercentage.ShouldBe(30m);
        rows.Single(r => r.HolderType == EquityHolderType.Esop).EquityPercentage.ShouldBe(10m);
    }

    [Fact]
    public async Task Replace_RemovesPreviousRows()
    {
        SetStartupCapTableCommandHandler sut = CreateSut(_founderAId);
        await sut.Handle(new SetStartupCapTableCommand(_startupId,
            [Founder(_founderAId, 60m), Founder(_founderBId, 30m), Esop(10m)]), CancellationToken.None);

        // Second call with a different split must fully replace, not accumulate.
        await sut.Handle(new SetStartupCapTableCommand(_startupId,
            [Founder(_founderAId, 80m), Esop(20m)]), CancellationToken.None);

        List<StartupEquityHolder> rows = await _db.StartupEquityHolders
            .Where(h => h.StartupId == _startupId).ToListAsync();
        rows.Count.ShouldBe(2);
        rows.Single(r => r.ProfileId == _founderAId).EquityPercentage.ShouldBe(80m);
    }

    [Fact]
    public async Task NonMember_IsUnauthorized()
    {
        SetStartupCapTableCommandHandler sut = CreateSut(Guid.NewGuid());
        var command = new SetStartupCapTableCommand(_startupId,
            [Founder(_founderAId, 90m), Esop(10m)]);

        Result result = await sut.Handle(command, CancellationToken.None);

        result.Error.ShouldBe(StartupEquityErrors.Unauthorized);
        (await _db.StartupEquityHolders.AnyAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task FounderRowForNonFounder_IsRejected()
    {
        SetStartupCapTableCommandHandler sut = CreateSut(_founderAId);
        // A founder row pointing at a profile that isn't a founder member.
        var command = new SetStartupCapTableCommand(_startupId,
            [Founder(_founderAId, 50m), Founder(Guid.NewGuid(), 40m), Esop(10m)]);

        Result result = await sut.Handle(command, CancellationToken.None);

        result.Error.ShouldBe(StartupEquityErrors.FounderNotAMember);
    }
}
