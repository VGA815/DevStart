using DevStart.Domain.StartupInvestors;
using Shouldly;

namespace DevStart.UnitTests.Domain.StartupInvestors;

public sealed class StartupInvestorTests
{
    [Fact]
    public void Create_ShouldInitializeStartupInvestor()
    {
        Guid profileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid startupId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        DateTime createdAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        StartupInvestor investor = StartupInvestor.Create(profileId, startupId, isPublic: true, createdAt);

        investor.ProfileId.ShouldBe(profileId);
        investor.StartupId.ShouldBe(startupId);
        investor.IsPublic.ShouldBeTrue();
        investor.CreatedAt.ShouldBe(createdAt);
        investor.UpdatedAt.ShouldBe(createdAt);
    }
}
