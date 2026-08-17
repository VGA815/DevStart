using DevStart.Application.Scoring.Benchmarks;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;
using Shouldly;

namespace DevStart.UnitTests.Application.Scoring;

/// <summary>
/// The derivation engine is a pure function, so every one of these runs without a database, a clock or
/// a network — which is the property that makes each link of the chain assertable on its own.
/// </summary>
public sealed class BenchmarkDerivationEngineTests
{
    private static readonly DateTime AsOf = new(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);

    private static BenchmarkDerivationParameters Parameters(
        int minComparables = 3,
        decimal countryDiscount = 0.60m,
        decimal illiquidity = 0.70m,
        string region = "Emerging Markets")
        => new(minComparables, countryDiscount, illiquidity, region, AsOf);

    private static BenchmarkDerivationInputs Inputs(
        IReadOnlyList<DamodaranBucketInput>? buckets = null,
        IReadOnlyDictionary<string, Industry?>? mappings = null,
        IReadOnlyList<ComparableInput>? comparables = null)
        => new(buckets ?? [], mappings ?? new Dictionary<string, Industry?>(), comparables ?? []);

    private static ComparableInput Comparable(string ticker, Industry industry, decimal cap, decimal revenue, int? fy = 2024)
        => new(ticker, industry, cap, revenue, fy, RevenueIsManual: false);

    private static BenchmarkSuggestion Multiple(IReadOnlyList<BenchmarkSuggestion> all, Industry industry) =>
        all.Single(s => s.MetricType == BenchmarkMetricType.RevenueMultiple && s.Industry == industry);

    [Fact]
    public void EveryIndustryGetsARevenueMultipleEntry_EvenWhenThereIsNothingToSuggest()
    {
        IReadOnlyList<BenchmarkSuggestion> result = BenchmarkDerivationEngine.Derive(Inputs(), Parameters());

        foreach (Industry industry in Enum.GetValues<Industry>())
        {
            Multiple(result, industry).Value.ShouldBeNull();
        }
    }

    [Fact]
    public void NoComparablesAndNoBucket_IsAnExplicitNoSuggestion_NotZero()
    {
        IReadOnlyList<BenchmarkSuggestion> result = BenchmarkDerivationEngine.Derive(Inputs(), Parameters());

        BenchmarkSuggestion saas = Multiple(result, Industry.Saas);
        saas.Value.ShouldBeNull();
        saas.Source.ShouldBeNull();
        saas.NoSuggestionReason.ShouldNotBeNullOrWhiteSpace();
        saas.ComparableCount.ShouldBe(0);
    }

    [Fact]
    public void BucketButTooFewComparables_UsesTheParameter_AndSaysSo()
    {
        var result = BenchmarkDerivationEngine.Derive(
            Inputs(
                buckets: [new DamodaranBucketInput("Software (System & Application)", 4.00m, 2025, "Emerging Markets")],
                mappings: new Dictionary<string, Industry?> { ["Software (System & Application)"] = Industry.Saas },
                comparables: [Comparable("POSI", Industry.Saas, 200m, 100m)]),
            Parameters(minComparables: 3));

        BenchmarkSuggestion saas = Multiple(result, Industry.Saas);

        // 4.00 × 0.60 × 0.70 = 1.68
        saas.Value.ShouldBe(1.68m);
        saas.IsDerived.ShouldBeFalse();
        saas.ComparableCount.ShouldBe(1);
        saas.Source!.ShouldContain("параметр");
    }

    [Fact]
    public void EnoughComparables_DerivesTheCountryCoefficient_AndLandsOnTheRussianMedian()
    {
        // Multiples 2.0, 3.0, 4.0 → median 3.0. Damodaran base 6.0 → coefficient 0.50.
        // 6.0 × 0.50 × 0.70 = 2.10, i.e. the Russian median discounted for illiquidity.
        var result = BenchmarkDerivationEngine.Derive(
            Inputs(
                buckets: [new DamodaranBucketInput("Software (System & Application)", 6.00m, 2025, "Emerging Markets")],
                mappings: new Dictionary<string, Industry?> { ["Software (System & Application)"] = Industry.Saas },
                comparables:
                [
                    Comparable("A", Industry.Saas, 200m, 100m),
                    Comparable("B", Industry.Saas, 300m, 100m),
                    Comparable("C", Industry.Saas, 400m, 100m),
                ]),
            Parameters(minComparables: 3));

        BenchmarkSuggestion saas = Multiple(result, Industry.Saas);

        saas.Value.ShouldBe(2.10m);
        saas.IsDerived.ShouldBeTrue();
        saas.ComparableCount.ShouldBe(3);
        saas.Source!.ShouldContain("выведен");
    }

    [Fact]
    public void NoBucketButEnoughComparables_TakesTheBaseStraightFromTheRussianMedian()
    {
        var result = BenchmarkDerivationEngine.Derive(
            Inputs(comparables:
            [
                Comparable("A", Industry.Fintech, 200m, 100m),
                Comparable("B", Industry.Fintech, 300m, 100m),
                Comparable("C", Industry.Fintech, 400m, 100m),
            ]),
            Parameters(minComparables: 3));

        BenchmarkSuggestion fintech = Multiple(result, Industry.Fintech);

        fintech.Value.ShouldBe(2.10m);   // median 3.0 × 0.70
        fintech.IsDerived.ShouldBeTrue();
    }

    [Fact]
    public void OnlyOneHalfOfAMultiple_DoesNotCount_TheCallerNeverSuppliesIt()
    {
        // A comparable with zero revenue is filtered out rather than dividing by zero.
        var result = BenchmarkDerivationEngine.Derive(
            Inputs(comparables: [Comparable("A", Industry.Saas, 200m, 0m)]),
            Parameters());

        Multiple(result, Industry.Saas).ComparableCount.ShouldBe(0);
    }

    [Fact]
    public void FiscalYearsOfTheRevenueUsed_TravelIntoTheOutputAndTheSource()
    {
        var result = BenchmarkDerivationEngine.Derive(
            Inputs(comparables:
            [
                Comparable("A", Industry.Saas, 200m, 100m, fy: 2023),
                Comparable("B", Industry.Saas, 300m, 100m, fy: 2024),
                Comparable("C", Industry.Saas, 400m, 100m, fy: 2024),
            ]),
            Parameters(minComparables: 3));

        BenchmarkSuggestion saas = Multiple(result, Industry.Saas);

        saas.FiscalYears.ShouldBe([2023, 2024]);
        saas.Source!.ShouldContain("FY2023/2024");
    }

    [Fact]
    public void TheChainCarriesEveryStepWithItsIntermediateValue()
    {
        var result = BenchmarkDerivationEngine.Derive(
            Inputs(
                buckets: [new DamodaranBucketInput("Retail (Online)", 2.00m, 2025, "Emerging Markets")],
                mappings: new Dictionary<string, Industry?> { ["Retail (Online)"] = Industry.Ecommerce }),
            Parameters());

        BenchmarkSuggestion ecommerce = Multiple(result, Industry.Ecommerce);

        ecommerce.Chain.Count.ShouldBe(5);
        ecommerce.Chain[0].Value.ShouldBe(2.00m);   // Damodaran base
        ecommerce.Chain[1].Value.ShouldBeNull();    // no Russian comparables
        ecommerce.Chain[2].Value.ShouldBe(0.60m);   // country coefficient (parameter)
        ecommerce.Chain[3].Value.ShouldBe(0.70m);   // illiquidity
        ecommerce.Chain[4].Value.ShouldBe(0.84m);   // result
    }

    [Fact]
    public void OnlyTheNewestDatasetYearIsUsed()
    {
        var result = BenchmarkDerivationEngine.Derive(
            Inputs(
                buckets:
                [
                    new DamodaranBucketInput("Retail (Online)", 2.00m, 2024, "Emerging Markets"),
                    new DamodaranBucketInput("Retail (Online)", 5.00m, 2025, "Emerging Markets"),
                ],
                mappings: new Dictionary<string, Industry?> { ["Retail (Online)"] = Industry.Ecommerce }),
            Parameters());

        // 5.00 × 0.60 × 0.70 = 2.10 — the 2024 vintage must not blend in.
        Multiple(result, Industry.Ecommerce).Value.ShouldBe(2.10m);
    }

    [Fact]
    public void ARegionThatIsNotStaged_YieldsNoSuggestionRatherThanTheWrongSlice()
    {
        var result = BenchmarkDerivationEngine.Derive(
            Inputs(
                buckets: [new DamodaranBucketInput("Retail (Online)", 5.00m, 2025, "Global")],
                mappings: new Dictionary<string, Industry?> { ["Retail (Online)"] = Industry.Ecommerce }),
            Parameters(region: "Emerging Markets"));

        Multiple(result, Industry.Ecommerce).Value.ShouldBeNull();
    }

    [Fact]
    public void AnUnmappedBucketDoesNotLeakIntoASector()
    {
        var result = BenchmarkDerivationEngine.Derive(
            Inputs(buckets: [new DamodaranBucketInput("Some New Bucket", 9.00m, 2025, "Emerging Markets")]),
            Parameters());

        foreach (Industry industry in Enum.GetValues<Industry>())
        {
            Multiple(result, industry).Value.ShouldBeNull();
        }
    }

    [Fact]
    public void ABucketMappedToNothing_IsExcludedRatherThanTreatedAsUnknown()
    {
        var result = BenchmarkDerivationEngine.Derive(
            Inputs(
                buckets: [new DamodaranBucketInput("Banks (Regional)", 9.00m, 2025, "Emerging Markets")],
                mappings: new Dictionary<string, Industry?> { ["Banks (Regional)"] = null }),
            Parameters());

        Multiple(result, Industry.Fintech).Value.ShouldBeNull();
    }

    [Fact]
    public void EverySuggestedSourceFitsTheColumn()
    {
        var result = BenchmarkDerivationEngine.Derive(
            Inputs(
                buckets: [new DamodaranBucketInput("Software (System & Application)", 4.00m, 2025, "Emerging Markets")],
                mappings: new Dictionary<string, Industry?> { ["Software (System & Application)"] = Industry.Saas },
                comparables: Enumerable.Range(0, 12)
                    .Select(i => Comparable($"TICKER{i}", Industry.Saas, 200m + i, 100m, 2020 + (i % 5)))
                    .ToList()),
            Parameters(minComparables: 3));

        foreach (BenchmarkSuggestion suggestion in result.Where(s => s.Value is not null))
        {
            suggestion.Source!.Length.ShouldBeLessThanOrEqualTo(BenchmarkDerivationEngine.SourceMaxLength);
        }
    }

    [Fact]
    public void EffectiveFromIsTheStartOfTheQuarterTheDataDescribes()
    {
        var result = BenchmarkDerivationEngine.Derive(Inputs(), Parameters());

        // AsOf is 17 August 2026 → Q3 starts 1 July.
        result[0].EffectiveFrom.ShouldBe(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CompetitionIntensityIsSuggestedForEverySector_AsAParameterNotADerivation()
    {
        IReadOnlyList<BenchmarkSuggestion> result = BenchmarkDerivationEngine.Derive(Inputs(), Parameters());

        List<BenchmarkSuggestion> intensities = result
            .Where(s => s.MetricType == BenchmarkMetricType.CompetitionIntensity)
            .ToList();

        intensities.Count.ShouldBe(Enum.GetValues<Industry>().Length);

        foreach (BenchmarkSuggestion suggestion in intensities)
        {
            suggestion.Value.ShouldNotBeNull();
            suggestion.Value!.Value.ShouldBeInRange(0m, 100m);
            suggestion.IsDerived.ShouldBeFalse();
            // The spread rule and the rank denominator have to be readable off the row itself.
            suggestion.Source!.ShouldContain("ранг");
            suggestion.Source!.ShouldContain("значение = 90");
            suggestion.Source!.Length.ShouldBeLessThanOrEqualTo(BenchmarkDerivationEngine.SourceMaxLength);
        }
    }

    [Fact]
    public void CompetitionIntensityIsStrictlyDecreasingDownTheRanking()
    {
        IReadOnlyList<BenchmarkSuggestion> result = BenchmarkDerivationEngine.Derive(Inputs(), Parameters());

        decimal[] values = CompetitionIntensityRanking.Ranking
            .Select(r => result
                .Single(s => s.MetricType == BenchmarkMetricType.CompetitionIntensity && s.Industry == r.Industry)
                .Value!.Value)
            .ToArray();

        values.ShouldBe(values.OrderByDescending(v => v).ToArray());
        values.Distinct().Count().ShouldBe(values.Length);
    }
}
