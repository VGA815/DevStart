using DevStart.Application.Scoring;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;
using Shouldly;

namespace DevStart.UnitTests.Application.Scoring;

public sealed class ValuationBenchmarkSetTests
{
    private static readonly DateTime AsOf = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static ValuationBenchmarkRow Median(Industry industry, StartupStage stage, decimal value, DateTime effectiveFrom) =>
        new(BenchmarkMetricType.PreMoneyMedian, industry, stage, value, effectiveFrom);

    private static ValuationBenchmarkRow Multiple(Industry industry, decimal value, DateTime effectiveFrom) =>
        new(BenchmarkMetricType.RevenueMultiple, industry, null, value, effectiveFrom);

    [Fact]
    public void FromRows_PicksLatestVersionNotAfterAsOf()
    {
        var set = ValuationBenchmarkSet.FromRows(
        [
            Median(Industry.Other, StartupStage.Seed, 300_000_000m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            Median(Industry.Other, StartupStage.Seed, 400_000_000m, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)),
        ], AsOf);

        set.Median(Industry.Other, StartupStage.Seed).ShouldBe(400_000_000m);
    }

    [Fact]
    public void FromRows_IgnoresFutureDatedVersions()
    {
        var set = ValuationBenchmarkSet.FromRows(
        [
            Median(Industry.Other, StartupStage.Seed, 400_000_000m, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)),
            Median(Industry.Other, StartupStage.Seed, 999_000_000m, new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)),
        ], AsOf);

        // The Dec version is after AsOf (June) and must not win.
        set.Median(Industry.Other, StartupStage.Seed).ShouldBe(400_000_000m);
    }

    [Fact]
    public void Median_FallsBackToOther_WhenNoSectorSpecificRow()
    {
        var set = ValuationBenchmarkSet.FromRows(
        [
            Median(Industry.Other, StartupStage.Seed, 400_000_000m, AsOf),
        ], AsOf);

        set.Median(Industry.Saas, StartupStage.Seed).ShouldBe(400_000_000m);
        set.HasSectorMedian(Industry.Saas, StartupStage.Seed).ShouldBeFalse();
    }

    [Fact]
    public void Median_PrefersSectorSpecificRow_OverOther()
    {
        var set = ValuationBenchmarkSet.FromRows(
        [
            Median(Industry.Other, StartupStage.Seed, 400_000_000m, AsOf),
            Median(Industry.Saas, StartupStage.Seed, 800_000_000m, AsOf),
        ], AsOf);

        set.Median(Industry.Saas, StartupStage.Seed).ShouldBe(800_000_000m);
        set.HasSectorMedian(Industry.Saas, StartupStage.Seed).ShouldBeTrue();
    }

    [Fact]
    public void Median_ReturnsNull_WhenNoDataAtAll()
    {
        ValuationBenchmarkSet.Empty.Median(Industry.Saas, StartupStage.Seed).ShouldBeNull();
    }

    [Fact]
    public void RevenueMultiple_PicksLatest_AndReturnsNullWhenAbsent()
    {
        var set = ValuationBenchmarkSet.FromRows(
        [
            Multiple(Industry.Saas, 5m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            Multiple(Industry.Saas, 6m, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)),
        ], AsOf);

        set.RevenueMultiple(Industry.Saas).ShouldBe(6m);
        set.RevenueMultiple(Industry.Fintech).ShouldBeNull();
    }

    [Fact]
    public void CompetitionIntensity_PicksLatestVersionNotAfterAsOf()
    {
        var set = ValuationBenchmarkSet.FromRows(
        [
            Intensity(Industry.Saas, 40m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            Intensity(Industry.Saas, 75m, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)),
            Intensity(Industry.Saas, 90m, new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)),
        ], AsOf);

        set.CompetitionIntensity(Industry.Saas).ShouldBe(75m);
    }

    [Fact]
    public void CompetitionIntensity_PrefersSectorRow_ThenFallsBackToOther_ThenNull()
    {
        var general = ValuationBenchmarkSet.FromRows([Intensity(Industry.Other, 30m, AsOf)], AsOf);
        general.CompetitionIntensity(Industry.Fintech).ShouldBe(30m);

        var sectorSpecific = ValuationBenchmarkSet.FromRows(
        [
            Intensity(Industry.Other, 30m, AsOf),
            Intensity(Industry.Fintech, 85m, AsOf),
        ], AsOf);
        sectorSpecific.CompetitionIntensity(Industry.Fintech).ShouldBe(85m);

        ValuationBenchmarkSet.Empty.CompetitionIntensity(Industry.Fintech).ShouldBeNull();
    }

    private static ValuationBenchmarkRow Intensity(Industry industry, decimal value, DateTime effectiveFrom) =>
        new(BenchmarkMetricType.CompetitionIntensity, industry, null, value, effectiveFrom);
}
