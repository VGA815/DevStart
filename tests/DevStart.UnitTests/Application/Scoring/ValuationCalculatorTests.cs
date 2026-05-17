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
        ValuationRange range = _calculator.ComputeRange(50m, stage);

        range.Low.ShouldBe(90_750_000m);
        range.High.ShouldBe(151_250_000m);
        range.MethodsUsed.ShouldBe(["Berkus", "Scorecard", "Expert"]);
    }

    [Theory]
    [InlineData(StartupStage.Mvp)]
    [InlineData(StartupStage.Seed)]
    public void ComputeRange_ShouldUseSeedStageMethods(StartupStage stage)
    {
        ValuationRange range = _calculator.ComputeRange(50m, stage);

        range.Low.ShouldBe(148_875_000m);
        range.High.ShouldBe(248_125_000m);
        range.MethodsUsed.ShouldBe(["Scorecard", "VcMethod", "Comparable", "Dcf"]);
    }

    [Fact]
    public void ComputeRange_ShouldUseSeriesAStageMethods()
    {
        ValuationRange range = _calculator.ComputeRange(50m, StartupStage.SeriesA);

        range.Low.ShouldBe(200_400_000m);
        range.High.ShouldBe(334_000_000m);
        range.MethodsUsed.ShouldBe(["VcMethod", "Dcf", "Comparable", "FirstChicago"]);
    }
}
