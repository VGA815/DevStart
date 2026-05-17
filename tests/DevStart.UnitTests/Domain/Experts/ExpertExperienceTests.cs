using DevStart.Domain.Experts;
using Shouldly;

namespace DevStart.UnitTests.Domain.Experts;

public sealed class ExpertExperienceTests
{
    [Fact]
    public void Create_ShouldInitializeExperience()
    {
        Guid expertProfileId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        DateTime createdAt = new(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);
        DateOnly start = new(2020, 1, 1);
        DateOnly end = new(2022, 12, 31);

        ExpertExperience experience = ExpertExperience.Create(
            expertProfileId,
            "Acme Corp",
            "Senior Engineer",
            start,
            end,
            "Built distributed systems.",
            createdAt);

        experience.Id.ShouldNotBe(Guid.Empty);
        experience.ExpertProfileId.ShouldBe(expertProfileId);
        experience.Company.ShouldBe("Acme Corp");
        experience.Position.ShouldBe("Senior Engineer");
        experience.StartDate.ShouldBe(start);
        experience.EndDate.ShouldBe(end);
        experience.Description.ShouldBe("Built distributed systems.");
        experience.CreatedAt.ShouldBe(createdAt);
        experience.UpdatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void Update_ShouldReplaceMutableFields()
    {
        ExpertExperience experience = ExpertExperience.Create(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Old Corp",
            "Junior",
            new DateOnly(2018, 1, 1),
            new DateOnly(2019, 12, 31),
            "old",
            new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc));
        DateTime updatedAt = new(2026, 5, 17, 11, 0, 0, DateTimeKind.Utc);
        DateOnly newStart = new(2020, 1, 1);

        experience.Update("New Corp", "Lead", newStart, endDate: null, description: null, updatedAt);

        experience.Company.ShouldBe("New Corp");
        experience.Position.ShouldBe("Lead");
        experience.StartDate.ShouldBe(newStart);
        experience.EndDate.ShouldBeNull();
        experience.Description.ShouldBeNull();
        experience.UpdatedAt.ShouldBe(updatedAt);
    }
}
