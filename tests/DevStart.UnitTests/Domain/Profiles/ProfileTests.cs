using DevStart.Domain.Profiles;
using Shouldly;

namespace DevStart.UnitTests.Domain.Profiles;

public sealed class ProfileTests
{
    [Fact]
    public void Create_ShouldInitializeProfile()
    {
        Guid userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid avatarId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        Profile profile = Profile.Create(
            userId,
            "Alice",
            "Bio",
            "https://example.com",
            isAvailableForHire: true,
            isPublic: false,
            avatarId);

        profile.UserId.ShouldBe(userId);
        profile.Name.ShouldBe("Alice");
        profile.Bio.ShouldBe("Bio");
        profile.Url.ShouldBe("https://example.com");
        profile.IsAvailableForHire.ShouldBeTrue();
        profile.IsPublic.ShouldBeFalse();
        profile.AvatarId.ShouldBe(avatarId);
        profile.SocialMediaLinks.ShouldBeEmpty();
    }
}
