using DevStart.Domain.Experts;
using Shouldly;

namespace DevStart.UnitTests.Domain.Experts;

public sealed class ExpertProfileTests
{
    [Fact]
    public void Create_ShouldInitializeExpertProfile()
    {
        Guid userId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        DateTime createdAt = new(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);

        ExpertProfile profile = ExpertProfile.Create(userId, createdAt);

        profile.Id.ShouldBe(userId);
        profile.UserId.ShouldBe(userId);
        profile.CreatedAt.ShouldBe(createdAt);
        profile.UpdatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void Touch_ShouldUpdateTimestamp()
    {
        ExpertProfile profile = ExpertProfile.Create(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc));
        DateTime updatedAt = new(2026, 5, 17, 11, 0, 0, DateTimeKind.Utc);

        profile.Touch(updatedAt);

        profile.UpdatedAt.ShouldBe(updatedAt);
    }
}
