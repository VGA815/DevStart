using DevStart.Application.Scoring.Benchmarks;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;
using DevStart.Infrastructure.Valuation;
using Shouldly;

namespace DevStart.UnitTests.Application.Scoring;

/// <summary>
/// The SC-58 coverage guarantee. Adding a value to <see cref="Industry"/> and nothing else fails these
/// on purpose: an unmapped sector is one whose Comparable method silently never fires, and a sector
/// missing from the competition ranking is one whose intensity would be undefined.
/// </summary>
public sealed class BenchmarkRegistryCoverageTests
{
    [Fact]
    public void EveryIndustry_HasADamodaranBucket_OrIsExplicitlyMarkedAsHavingNoComparables()
    {
        HashSet<Industry> mapped = BenchmarkRegistryDefaults.DamodaranBuckets
            .Where(b => b.Industry.HasValue)
            .Select(b => b.Industry!.Value)
            .ToHashSet();

        var uncovered = new List<Industry>();

        foreach (Industry industry in Enum.GetValues<Industry>())
        {
            if (!mapped.Contains(industry)
                && !BenchmarkRegistryDefaults.IndustriesWithoutDamodaranBucket.ContainsKey(industry))
            {
                uncovered.Add(industry);
            }
        }

        uncovered.ShouldBeEmpty(
            $"Sector(s) {string.Join(", ", uncovered)} have neither a mapped Damodaran bucket nor an "
            + "entry in IndustriesWithoutDamodaranBucket. Add one or the other — a sector in neither "
            + "place has no Comparable method and nothing says so.");
    }

    [Fact]
    public void ASectorIsNeverBothMappedAndDeclaredWithoutComparables()
    {
        HashSet<Industry> mapped = BenchmarkRegistryDefaults.DamodaranBuckets
            .Where(b => b.Industry.HasValue)
            .Select(b => b.Industry!.Value)
            .ToHashSet();

        foreach (Industry industry in BenchmarkRegistryDefaults.IndustriesWithoutDamodaranBucket.Keys)
        {
            mapped.ShouldNotContain(industry);
        }
    }

    [Fact]
    public void DamodaranBucketNamesAreUnique_TheyAreTheNaturalKey()
    {
        string[] keys = BenchmarkRegistryDefaults.DamodaranBuckets.Select(b => b.ExternalKey).ToArray();

        keys.Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(keys.Length);
    }

    [Fact]
    public void IssuerTickersAreUnique()
    {
        string[] tickers = BenchmarkRegistryDefaults.Issuers.Select(i => i.Ticker).ToArray();

        tickers.Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(tickers.Length);
    }

    [Fact]
    public void SeededIssuersCarryNoInn_BecauseAGuessedOneWouldPullAnotherCompanysRevenue()
    {
        var utcNow = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // The rows the seeder actually writes, not the seed model — this is the policy under test.
        // An admin fills the INN in (or enters a consolidated override instead); changing that has to
        // be a deliberate act that breaks this test first.
        foreach (BenchmarkRegistryDefaults.IssuerSeed seed in BenchmarkRegistryDefaults.Issuers)
        {
            BenchmarkIssuer issuer = BenchmarkRegistryDefaults.ToIssuer(seed, utcNow);

            issuer.Inn.ShouldBeNull($"{seed.Ticker} is seeded with an INN.");
            issuer.Ticker.ShouldBe(seed.Ticker);
            issuer.Industry.ShouldBe(seed.Industry);
            issuer.IsActive.ShouldBeTrue();

            // No override either: a seeded revenue would be a figure with no stated provenance.
            issuer.RevenueOverride.ShouldBeNull();
        }
    }

    [Fact]
    public void SeededNotesFitTheColumn()
    {
        BenchmarkRegistryDefaults.Issuers.ShouldAllBe(i => i.Note == null || i.Note.Length <= 512);
        BenchmarkRegistryDefaults.DamodaranBuckets.ShouldAllBe(b => b.Note == null || b.Note.Length <= 512);
    }

    [Fact]
    public void EveryIndustryAppearsExactlyOnceInTheCompetitionRanking()
    {
        Industry[] ranked = CompetitionIntensityRanking.Ranking.Select(r => r.Industry).ToArray();

        ranked.Length.ShouldBe(Enum.GetValues<Industry>().Length);
        ranked.Distinct().Count().ShouldBe(ranked.Length);

        foreach (Industry industry in Enum.GetValues<Industry>())
        {
            ranked.ShouldContain(industry);
        }
    }

    [Fact]
    public void EveryRankedSectorHasAStatedBasis()
    {
        foreach ((Industry industry, string basis) in CompetitionIntensityRanking.Ranking)
        {
            basis.ShouldNotBeNullOrWhiteSpace($"{industry} has no stated basis for its rank.");
        }
    }

    [Fact]
    public void TheRankingSpreadStaysInsideTheValidatorsRange()
    {
        for (int rank = 1; rank <= CompetitionIntensityRanking.Ranking.Count; rank++)
        {
            CompetitionIntensityRanking.ValueForRank(rank).ShouldBeInRange(0m, 100m);
        }
    }
}
