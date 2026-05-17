using DevStart.Domain.StartupFollowers;
using Shouldly;

namespace DevStart.UnitTests.Domain.StartupFollowers;

public sealed class StartupFollowerTests
{
    [Fact]
    public void Create_ShouldInitializeStartupFollower()
    {
        Guid profileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid startupId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        DateTime createdAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        StartupFollower follower = StartupFollower.Create(profileId, startupId, createdAt);

        follower.ProfileId.ShouldBe(profileId);
        follower.StartupId.ShouldBe(startupId);
        follower.CreatedAt.ShouldBe(createdAt);
    }
}
