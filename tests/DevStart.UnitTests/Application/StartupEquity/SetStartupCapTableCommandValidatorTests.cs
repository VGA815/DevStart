using DevStart.Application.StartupEquity.SetCapTable;
using DevStart.Domain.StartupEquity;
using Shouldly;

namespace DevStart.UnitTests.Application.StartupEquity;

public sealed class SetStartupCapTableCommandValidatorTests
{
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly SetStartupCapTableCommandValidator _validator = new();

    private static CapTableHolderInput Founder(decimal pct, Guid? id = null) =>
        new(EquityHolderType.Founder, id ?? Guid.NewGuid(), null, pct, null, null, null);

    private static CapTableHolderInput Esop(decimal pct) =>
        new(EquityHolderType.Esop, null, "ESOP pool", pct, null, null, null);

    private static SetStartupCapTableCommand Command(params CapTableHolderInput[] holders) =>
        new(Guid.NewGuid(), holders);

    [Fact]
    public void ValidHundredPercent_Passes()
    {
        _validator.Validate(Command(Founder(60m), Founder(30m), Esop(10m))).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SumNot100_Fails()
    {
        _validator.Validate(Command(Founder(60m), Founder(30m))).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_Fails()
    {
        _validator.Validate(Command()).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void DuplicateProfile_Fails()
    {
        Guid dup = Guid.NewGuid();
        _validator.Validate(Command(Founder(50m, dup), Founder(40m, dup), Esop(10m))).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void FounderWithoutProfile_Fails()
    {
        var founderNoId = new CapTableHolderInput(EquityHolderType.Founder, null, "x", 90m, null, null, null);
        _validator.Validate(Command(founderNoId, Esop(10m))).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void NonFounderWithoutName_Fails()
    {
        var esopNoName = new CapTableHolderInput(EquityHolderType.Esop, null, null, 10m, null, null, null);
        _validator.Validate(Command(Founder(90m), esopNoName)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void VestingStartWithoutDuration_Fails()
    {
        var badVesting = new CapTableHolderInput(EquityHolderType.Founder, Guid.NewGuid(), null, 90m, Start, null, null);
        _validator.Validate(Command(badVesting, Esop(10m))).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CliffLongerThanDuration_Fails()
    {
        var badCliff = new CapTableHolderInput(EquityHolderType.Founder, Guid.NewGuid(), null, 90m, Start, 24, 36);
        _validator.Validate(Command(badCliff, Esop(10m))).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void ValidVesting_Passes()
    {
        var vesting = new CapTableHolderInput(EquityHolderType.Founder, Guid.NewGuid(), null, 90m, Start, 48, 12);
        _validator.Validate(Command(vesting, Esop(10m))).IsValid.ShouldBeTrue();
    }
}
