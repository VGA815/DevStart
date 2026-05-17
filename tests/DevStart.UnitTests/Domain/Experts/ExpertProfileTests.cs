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

        ExpertProfile profile = ExpertProfile.Create(
            userId,
            "Expert Name",
            "Bio",
            "https://expert.example.com",
            isPublic: true,
            linkedInUrl: "https://linkedin.com/in/expert",
            twitterUrl: "https://x.com/expert",
            gitHubUrl: "https://github.com/expert",
            telegramUrl: "https://t.me/expert",
            createdAt);

        profile.Id.ShouldBe(userId);
        profile.UserId.ShouldBe(userId);
        profile.DisplayName.ShouldBe("Expert Name");
        profile.Bio.ShouldBe("Bio");
        profile.Website.ShouldBe("https://expert.example.com");
        profile.IsPublic.ShouldBeTrue();
        profile.LinkedInUrl.ShouldBe("https://linkedin.com/in/expert");
        profile.TwitterUrl.ShouldBe("https://x.com/expert");
        profile.GitHubUrl.ShouldBe("https://github.com/expert");
        profile.TelegramUrl.ShouldBe("https://t.me/expert");
        profile.CreatedAt.ShouldBe(createdAt);
        profile.UpdatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void Update_ShouldReplaceMutableFields()
    {
        ExpertProfile profile = ExpertProfile.Create(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Expert",
            "Bio",
            "https://expert.example.com",
            isPublic: false,
            linkedInUrl: null,
            twitterUrl: null,
            gitHubUrl: null,
            telegramUrl: null,
            new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc));
        DateTime updatedAt = new(2026, 5, 17, 11, 0, 0, DateTimeKind.Utc);

        profile.Update(
            "Updated Expert",
            "Updated Bio",
            website: null,
            isPublic: true,
            linkedInUrl: "https://linkedin.com/in/updated",
            twitterUrl: null,
            gitHubUrl: "https://github.com/updated",
            telegramUrl: null,
            updatedAt);

        profile.DisplayName.ShouldBe("Updated Expert");
        profile.Bio.ShouldBe("Updated Bio");
        profile.Website.ShouldBeNull();
        profile.IsPublic.ShouldBeTrue();
        profile.LinkedInUrl.ShouldBe("https://linkedin.com/in/updated");
        profile.TwitterUrl.ShouldBeNull();
        profile.GitHubUrl.ShouldBe("https://github.com/updated");
        profile.TelegramUrl.ShouldBeNull();
        profile.UpdatedAt.ShouldBe(updatedAt);
    }
}
