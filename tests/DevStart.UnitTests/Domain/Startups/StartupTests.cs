using DevStart.Domain.Startups;
using Shouldly;

namespace DevStart.UnitTests.Domain.Startups;

public sealed class StartupTests
{
    [Fact]
    public void Create_ShouldInitializeStartupWithDefaultsAndMarketFields()
    {
        DateTime createdAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);
        Guid avatarId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        List<string> socialLinks = ["https://example.com/social"];

        Startup startup = Startup.Create(
            "DevStart",
            "public@example.com",
            "Description",
            "https://example.com",
            StartupStage.Mvp,
            StartupLocation.Russia,
            "billing@example.com",
            avatarId,
            createdAt,
            socialLinks,
            "Short",
            tam: 1_000_000_000m,
            sam: 100_000_000m,
            som: 10_000_000m,
            marketGrowthRate: 15m,
            hasPatents: true);

        startup.Id.ShouldNotBe(Guid.Empty);
        startup.Name.ShouldBe("DevStart");
        startup.PublicEmail.ShouldBe("public@example.com");
        startup.ShortDescription.ShouldBe("Short");
        startup.Description.ShouldBe("Description");
        startup.Url.ShouldBe("https://example.com");
        startup.Stage.ShouldBe(StartupStage.Mvp);
        startup.Location.ShouldBe(StartupLocation.Russia);
        startup.BillingEmail.ShouldBe("billing@example.com");
        startup.AvatarId.ShouldBe(avatarId);
        startup.SocialMediaLinks.ShouldBe(socialLinks);
        startup.Tam.ShouldBe(1_000_000_000m);
        startup.Sam.ShouldBe(100_000_000m);
        startup.Som.ShouldBe(10_000_000m);
        startup.MarketGrowthRate.ShouldBe(15m);
        startup.HasPatents.ShouldBeTrue();
        startup.IsStopped.ShouldBeFalse();
        startup.CreatedAt.ShouldBe(createdAt);
        startup.UpdatedAt.ShouldBe(createdAt);
    }
}
