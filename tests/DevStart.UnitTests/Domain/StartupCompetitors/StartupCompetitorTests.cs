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
    }

    [Fact]
    public void Update_ShouldReplaceCompetitorFields()
    {
        StartupCompetitor competitor = StartupCompetitor.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Competitor",
            null,
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
    }
}
