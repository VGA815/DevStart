using DevStart.Domain.StartupMembers;
using Shouldly;

namespace DevStart.UnitTests.Domain.StartupMembers;

public sealed class StartupMemberTests
{
    [Fact]
    public void Create_ShouldInitializeMemberProfileFields()
    {
        Guid profileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid startupId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        DateTime createdAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

        StartupMember member = StartupMember.Create(
            profileId,
            startupId,
            StartupRole.Founder,
            isPublic: true,
            createdAt,
            StartupPosition.CEO,
            "Founder bio",
            yearsOfExperience: 7,
            hasPriorExit: true,
            previousStartupsCount: 2);

        member.ProfileId.ShouldBe(profileId);
        member.StartupId.ShouldBe(startupId);
        member.Role.ShouldBe(StartupRole.Founder);
        member.IsPublic.ShouldBeTrue();
        member.Position.ShouldBe(StartupPosition.CEO);
        member.Bio.ShouldBe("Founder bio");
        member.YearsOfExperience.ShouldBe(7);
        member.HasPriorExit.ShouldBe(true);
        member.PreviousStartupsCount.ShouldBe(2);
        member.CreatedAt.ShouldBe(createdAt);
        member.UpdatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void UpdateProfile_ShouldReplaceProfileFieldsAndUpdateTimestamp()
    {
        StartupMember member = StartupMember.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            StartupRole.Member,
            isPublic: false,
            new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc));
        DateTime updatedAt = new(2026, 5, 16, 11, 0, 0, DateTimeKind.Utc);

        var result = member.UpdateProfile(
            StartupPosition.CTO,
            "Updated bio",
            yearsOfExperience: 5,
            hasPriorExit: false,
            previousStartupsCount: 1,
            updatedAt);

        result.IsSuccess.ShouldBeTrue();
        member.Position.ShouldBe(StartupPosition.CTO);
        member.Bio.ShouldBe("Updated bio");
        member.YearsOfExperience.ShouldBe(5);
        member.HasPriorExit.ShouldBe(false);
        member.PreviousStartupsCount.ShouldBe(1);
        member.UpdatedAt.ShouldBe(updatedAt);
    }
}
