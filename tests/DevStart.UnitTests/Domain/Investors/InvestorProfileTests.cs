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

        InvestorProfile profile = InvestorProfile.Create(
            userId,
            InvestorProfileType.Fund,
            "Fund",
            "Bio",
            "https://fund.example.com",
            isPublic: true,
            createdAt);

        profile.Id.ShouldBe(userId);
        profile.UserId.ShouldBe(userId);
        profile.Type.ShouldBe(InvestorProfileType.Fund);
        profile.DisplayName.ShouldBe("Fund");
        profile.Bio.ShouldBe("Bio");
        profile.Website.ShouldBe("https://fund.example.com");
        profile.IsPublic.ShouldBeTrue();
        profile.CreatedAt.ShouldBe(createdAt);
        profile.UpdatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void Update_ShouldReplaceMutableFields()
    {
        InvestorProfile profile = InvestorProfile.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            InvestorProfileType.Individual,
            "Investor",
            "Bio",
            "https://investor.example.com",
            isPublic: false,
            new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc));
        DateTime updatedAt = new(2026, 5, 16, 11, 0, 0, DateTimeKind.Utc);

        profile.Update(InvestorProfileType.Fund, "Fund", "Updated", null, isPublic: true, updatedAt);

        profile.Type.ShouldBe(InvestorProfileType.Fund);
        profile.DisplayName.ShouldBe("Fund");
        profile.Bio.ShouldBe("Updated");
        profile.Website.ShouldBeNull();
        profile.IsPublic.ShouldBeTrue();
        profile.UpdatedAt.ShouldBe(updatedAt);
    }
}
