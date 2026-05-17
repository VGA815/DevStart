using DevStart.Domain.StartupRoadmapItems;
using DevStart.Domain.Startups;
using Shouldly;

namespace DevStart.UnitTests.Domain.StartupRoadmapItems;

public sealed class StartupRoadmapItemTests
{
    [Fact]
    public void Create_ShouldInitializeRoadmapItem()
    {
        Guid startupId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        DateTime createdAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);
        DateTime targetDate = new(2026, 8, 16, 10, 0, 0, DateTimeKind.Utc);

        StartupRoadmapItem item = StartupRoadmapItem.Create(
            startupId,
            StartupStage.Seed,
            "Launch",
            "Launch product",
            RoadmapItemStatus.InProgress,
            5_000_000m,
            createdAt,
            targetDate);

        item.Id.ShouldNotBe(Guid.Empty);
        item.StartupId.ShouldBe(startupId);
        item.StartupStage.ShouldBe(StartupStage.Seed);
        item.Title.ShouldBe("Launch");
        item.Desription.ShouldBe("Launch product");
        item.Status.ShouldBe(RoadmapItemStatus.InProgress);
        item.TargetAmount.ShouldBe(5_000_000m);
        item.CreatedAt.ShouldBe(createdAt);
        item.TargetDate.ShouldBe(targetDate);
    }
}
