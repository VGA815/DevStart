using DevStart.Domain.Investors;
using Shouldly;

namespace DevStart.UnitTests.Domain.Investors;

public sealed class InvestorProfileTests
{
    [Fact]
    public void Create_ShouldInitializeInvestorProfile()
    {
        Guid userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        DateTime createdAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        InvestorProfile profile = InvestorProfile.Create(userId, InvestorProfileType.Fund, createdAt);

        profile.Id.ShouldBe(userId);
        profile.UserId.ShouldBe(userId);
        profile.Type.ShouldBe(InvestorProfileType.Fund);
        profile.CreatedAt.ShouldBe(createdAt);
        profile.UpdatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void Update_ShouldReplaceTypeAndTimestamp()
    {
        InvestorProfile profile = InvestorProfile.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            InvestorProfileType.Individual,
            new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc));
        DateTime updatedAt = new(2026, 5, 16, 11, 0, 0, DateTimeKind.Utc);

        profile.Update(InvestorProfileType.Fund, updatedAt);

        profile.Type.ShouldBe(InvestorProfileType.Fund);
        profile.UpdatedAt.ShouldBe(updatedAt);
    }

    [Fact]
    public void Create_ShouldKeepTheAvatar_WhenTheInvestorIsAFund()
    {
        Guid avatarId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        InvestorProfile profile = InvestorProfile.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            InvestorProfileType.Fund,
            new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc),
            avatarId);

        profile.AvatarId.ShouldBe(avatarId);
    }

    [Fact]
    public void Create_ShouldDropTheAvatar_WhenTheInvestorIsAnIndividual()
    {
        InvestorProfile profile = InvestorProfile.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            InvestorProfileType.Individual,
            new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        // Аватарка физлица живёт на общем Profile — своей у инвестор-профиля быть не должно.
        profile.AvatarId.ShouldBeNull();
    }

    [Fact]
    public void Update_ShouldClearTheAvatar_WhenTheTypeSwitchesToIndividual()
    {
        InvestorProfile profile = InvestorProfile.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            InvestorProfileType.Fund,
            new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        profile.Update(
            InvestorProfileType.Individual,
            new DateTime(2026, 5, 16, 11, 0, 0, DateTimeKind.Utc),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        profile.AvatarId.ShouldBeNull();
    }
}
