using DevStart.Application;
using DevStart.Application.Scoring;
using DevStart.Domain.Startups;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Scoring;

public sealed class ValuationCalculatorTests
{
    private readonly IValuationCalculator _calculator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IValuationCalculator>();

    [Theory]
    [InlineData(StartupStage.Idea)]
    [InlineData(StartupStage.PreSeed)]
    public void ComputeRange_ShouldUseEarlyStageMethods(StartupStage stage)
    {
        ValuationRange range = _calculator.ComputeRange(50m, stage, annualRecurringRevenue: 0m);

        range.Low.ShouldBe(90_750_000m);
        range.High.ShouldBe(151_250_000m);
        range.MethodsUsed.ShouldBe(["Berkus", "Scorecard", "Expert"]);
    }

    [Theory]
    [InlineData(StartupStage.Mvp)]
    [InlineData(StartupStage.Seed)]
    public void ComputeRange_ShouldUseSeedStageMethods(StartupStage stage)
    {
        // Pre-revenue (ARR = 0): Comparable falls back to its score-scaled proxy.
        ValuationRange range = _calculator.ComputeRange(50m, stage, annualRecurringRevenue: 0m);

        range.Low.ShouldBe(148_875_000m);
        range.High.ShouldBe(248_125_000m);
        range.MethodsUsed.ShouldBe(["Scorecard", "VcMethod", "Comparable", "Dcf"]);
    }

    [Fact]
    public void ComputeRange_ShouldAnchorComparableToRevenue_WhenSeedStageHasArr()
    {
        // Seed comparable = ARR × 8. With ARR = ₽100M → comparable = ₽800M (vs ₽225M score-scaled),
        // lifting the blended range above the pre-revenue case.
        ValuationRange range = _calculator.ComputeRange(50m, StartupStage.Seed, annualRecurringRevenue: 100_000_000m);

        // blended = 120M*.3 + 200M*.3 + 800M*.3 + 350M*.1 = ₽371M → ±25%
        range.Low.ShouldBe(278_250_000m);
        range.High.ShouldBe(463_750_000m);
        range.MethodsUsed.ShouldBe(["Scorecard", "VcMethod", "Comparable", "Dcf"]);
    }

    [Fact]
    public void ComputeRange_ShouldUseSeriesAStageMethods()
    {
        ValuationRange range = _calculator.ComputeRange(50m, StartupStage.SeriesA, annualRecurringRevenue: 0m);

        range.Low.ShouldBe(200_400_000m);
        range.High.ShouldBe(334_000_000m);
        range.MethodsUsed.ShouldBe(["VcMethod", "Dcf", "Comparable", "FirstChicago"]);
    }
}
