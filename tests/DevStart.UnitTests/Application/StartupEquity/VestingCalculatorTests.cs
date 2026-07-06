using DevStart.Application.StartupEquity.Vesting;
using Shouldly;

namespace DevStart.UnitTests.Application.StartupEquity;

public sealed class VestingCalculatorTests
{
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly IVestingCalculator _calculator = new VestingCalculator();

    [Fact]
    public void NoSchedule_IsFullyVested()
    {
        _calculator.VestedFraction(null, null, null, Start.AddYears(1)).ShouldBe(1m);
    }

    [Fact]
    public void ZeroDuration_IsTreatedAsNoSchedule()
    {
        _calculator.VestedFraction(Start, 0, 0, Start.AddYears(1)).ShouldBe(1m);
    }

    [Fact]
    public void AsOfBeforeStart_NothingVested()
    {
        _calculator.VestedFraction(Start, 48, 12, Start.AddMonths(-1)).ShouldBe(0m);
    }

    [Fact]
    public void BeforeCliff_NothingVested()
    {
        _calculator.VestedFraction(Start, 48, 12, Start.AddMonths(6)).ShouldBe(0m);
    }

    [Fact]
    public void AtCliff_ReleasesAccruedLinearPortion()
    {
        // 12 of 48 months elapsed at the cliff → 25% vests at once.
        _calculator.VestedFraction(Start, 48, 12, Start.AddMonths(12)).ShouldBe(0.25m);
    }

    [Fact]
    public void MidVesting_IsLinear()
    {
        _calculator.VestedFraction(Start, 48, 12, Start.AddMonths(24)).ShouldBe(0.5m);
    }

    [Fact]
    public void NoCliff_AccruesFromStart()
    {
        // 6 of 48 months, no cliff → 12.5%.
        _calculator.VestedFraction(Start, 48, null, Start.AddMonths(6)).ShouldBe(6m / 48m);
    }

    [Fact]
    public void AfterFullTerm_IsFullyVested()
    {
        _calculator.VestedFraction(Start, 48, 12, Start.AddMonths(60)).ShouldBe(1m);
    }

    [Fact]
    public void ExactlyAtTerm_IsFullyVested()
    {
        _calculator.VestedFraction(Start, 48, 12, Start.AddMonths(48)).ShouldBe(1m);
    }
}
