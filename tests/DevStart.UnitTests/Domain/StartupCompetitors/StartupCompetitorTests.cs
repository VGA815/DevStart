using DevStart.Domain.StartupCompetitors;
using Shouldly;

namespace DevStart.UnitTests.Domain.StartupCompetitors;

public sealed class StartupCompetitorTests
{
    [Fact]
    public void Create_ShouldInitializeCompetitor()
    {
        Guid startupId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        DateTime createdAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        StartupCompetitor competitor = StartupCompetitor.Create(
            startupId,
            "Competitor",
            "https://competitor.example.com",
            "Description",
            "Our strengths",
            "Their weaknesses",
            createdAt);

        competitor.Id.ShouldNotBe(Guid.Empty);
        competitor.StartupId.ShouldBe(startupId);
        competitor.Name.ShouldBe("Competitor");
        competitor.Website.ShouldBe("https://competitor.example.com");
        competitor.Description.ShouldBe("Description");
        competitor.StrengthsVsUs.ShouldBe("Our strengths");
        competitor.WeaknessesVsUs.ShouldBe("Their weaknesses");
        competitor.CreatedAt.ShouldBe(createdAt);
        competitor.UpdatedAt.ShouldBe(createdAt);
        competitor.NormalizedDomain.ShouldBe("competitor.example.com");
    }

    [Fact]
    public void Update_ShouldReplaceCompetitorFields()
    {
        StartupCompetitor competitor = StartupCompetitor.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Competitor",
            "https://competitor.example.com",
            null,
            null,
            null,
            new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc));
        DateTime updatedAt = new(2026, 5, 16, 11, 0, 0, DateTimeKind.Utc);

        competitor.Update("Updated", "https://updated.example.com", "Description", "Strengths", "Weaknesses", updatedAt);

        competitor.Name.ShouldBe("Updated");
        competitor.Website.ShouldBe("https://updated.example.com");
        competitor.Description.ShouldBe("Description");
        competitor.StrengthsVsUs.ShouldBe("Strengths");
        competitor.WeaknessesVsUs.ShouldBe("Weaknesses");
        competitor.UpdatedAt.ShouldBe(updatedAt);
        competitor.NormalizedDomain.ShouldBe("updated.example.com");
    }

    [Theory]
    [InlineData("https://rival.com", "rival.com")]
    [InlineData("http://rival.com", "rival.com")]
    [InlineData("https://WWW.Rival.COM", "rival.com")]
    [InlineData("https://www.rival.com/pricing?ref=x", "rival.com")]
    [InlineData("https://rival.com.", "rival.com")]
    [InlineData("  https://rival.com  ", "rival.com")]
    [InlineData("https://blog.rival.com", "blog.rival.com")]
    public void NormalizeDomain_ShouldReduceUrlToItsComparableDomain(string website, string expected)
    {
        StartupCompetitor.NormalizeDomain(website).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("rival.com")]          // not absolute
    [InlineData("ftp://rival.com")]    // not http(s)
    [InlineData("javascript:alert(1)")]
    public void NormalizeDomain_ShouldReturnNull_ForAnythingThatIsNotAnHttpUrl(string? website)
    {
        StartupCompetitor.NormalizeDomain(website).ShouldBeNull();
    }
}
