using DevStart.Application.ExpertProfiles.Create;
using DevStart.Application.ExpertProfiles.GetById;
using DevStart.Application.ExpertProfiles.Update;
using DevStart.Domain.Experts;
using DevStart.Domain.Profiles;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.UnitTests.Application.ExpertProfiles;

/// <summary>
/// The expert dashboard edits one form but two entities: specializations live on ExpertProfile,
/// everything personal on the shared Profile. These assert the write path stores what the read path
/// hands back — the two used to disagree, and the personal fields were dropped without an error.
/// </summary>
public sealed class ExpertProfileHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc);
    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FixedDateTimeProvider _clock = new() { UtcNow = Now };

    [Fact]
    public async Task Create_ShouldStoreThePersonalFieldsOnTheSharedProfile()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);

        Result<Guid> result = await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        Profile profile = await _db.Profiles.SingleAsync(p => p.UserId == userId);
        profile.Name.ShouldBe("Jane Expert");
        profile.Bio.ShouldBe("15 лет в продукте");
        profile.Url.ShouldBe("https://jane.dev");
        profile.IsPublic.ShouldBeFalse();
        profile.LinkedInUrl.ShouldBe("https://linkedin.com/in/jane");
        profile.TwitterUrl.ShouldBe("https://x.com/jane");
        profile.GitHubUrl.ShouldBe("https://github.com/jane");
        profile.TelegramUrl.ShouldBe("https://t.me/jane");
    }

    [Fact]
    public async Task Create_ShouldRoundTripThroughTheReadModel()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);

        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        Result<ExpertProfileResponse> read = await new GetExpertProfileByIdQueryHandler(_db)
            .Handle(new GetExpertProfileByIdQuery(userId), CancellationToken.None);

        read.IsSuccess.ShouldBeTrue();
        read.Value.DisplayName.ShouldBe("Jane Expert");
        read.Value.Bio.ShouldBe("15 лет в продукте");
        read.Value.Website.ShouldBe("https://jane.dev");
        read.Value.IsPublic.ShouldBeFalse();
        read.Value.TelegramUrl.ShouldBe("https://t.me/jane");
        read.Value.Specializations.ShouldBe([ExpertSpecialization.Product], ignoreOrder: true);
    }

    [Fact]
    public async Task Create_ShouldLeaveTheFieldsTheExpertFormDoesNotEdit()
    {
        Guid userId = Guid.NewGuid();
        Guid avatarId = Guid.NewGuid();
        Profile seeded = SeedProfile(userId);
        seeded.AvatarId = avatarId;
        seeded.IsAvailableForHire = true;
        seeded.SocialMediaLinks = ["https://vc.ru/jane"];
        await _db.SaveChangesAsync();

        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        Profile profile = await _db.Profiles.SingleAsync(p => p.UserId == userId);
        profile.AvatarId.ShouldBe(avatarId);
        profile.IsAvailableForHire.ShouldBeTrue();
        profile.SocialMediaLinks.ShouldBe(["https://vc.ru/jane"]);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenTheUserHasNoProfileRow()
    {
        Guid userId = Guid.NewGuid();

        Result<Guid> result = await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Profiles.NotFound");
    }

    [Fact]
    public async Task Create_ShouldFail_WhenAnExpertProfileAlreadyExists()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);
        _db.ExpertProfiles.Add(ExpertProfile.Create(userId, Now));
        await _db.SaveChangesAsync();

        Result<Guid> result = await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertProfileErrors.AlreadyExists(userId));
    }

    [Fact]
    public async Task Update_ShouldOverwriteThePersonalFieldsAndSpecializations()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);
        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        Result result = await UpdateHandler(userId).Handle(
            new UpdateExpertProfileCommand(
                [ExpertSpecialization.Finance, ExpertSpecialization.Legal],
                displayName: "Jane E.",
                bio: "Обновлённое био",
                website: "https://jane.example",
                isPublic: true,
                linkedInUrl: null,
                twitterUrl: null,
                gitHubUrl: null,
                telegramUrl: null),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        Profile profile = await _db.Profiles.SingleAsync(p => p.UserId == userId);
        profile.Name.ShouldBe("Jane E.");
        profile.Bio.ShouldBe("Обновлённое био");
        profile.Url.ShouldBe("https://jane.example");
        profile.IsPublic.ShouldBeTrue();
        // Cleared in the form means cleared in storage, not left at the previous value.
        profile.LinkedInUrl.ShouldBeNull();
        profile.TelegramUrl.ShouldBeNull();

        List<ExpertSpecialization> specializations = await _db.ExpertProfileSpecializations
            .Where(s => s.ExpertProfileId == profile.UserId)
            .Select(s => s.Specialization)
            .ToListAsync();
        specializations.ShouldBe([ExpertSpecialization.Finance, ExpertSpecialization.Legal], ignoreOrder: true);
    }

    [Fact]
    public async Task Update_ShouldStoreBlankOptionalFieldsAsNull()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);
        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        await UpdateHandler(userId).Handle(
            new UpdateExpertProfileCommand(
                [ExpertSpecialization.Product],
                displayName: "  Jane Expert  ",
                bio: "   ",
                website: "",
                isPublic: true),
            CancellationToken.None);

        Profile profile = await _db.Profiles.SingleAsync(p => p.UserId == userId);
        profile.Name.ShouldBe("Jane Expert");
        profile.Bio.ShouldBeNull();
        profile.Url.ShouldBeNull();
    }

    [Fact]
    public async Task Update_ShouldFail_WhenNoExpertProfileExists()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);

        Result result = await UpdateHandler(userId).Handle(
            new UpdateExpertProfileCommand([ExpertSpecialization.Product], displayName: "Jane"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertProfileErrors.NotFound(userId));
    }

    private CreateExpertProfileCommandHandler CreateHandler(Guid userId) =>
        new(_db, new TestUserContext(userId), _clock);

    private UpdateExpertProfileCommandHandler UpdateHandler(Guid userId) =>
        new(_db, new TestUserContext(userId), _clock);

    private static CreateExpertProfileCommand CreateCommand() =>
        new([ExpertSpecialization.Product],
            displayName: "Jane Expert",
            bio: "15 лет в продукте",
            website: "https://jane.dev",
            isPublic: false,
            linkedInUrl: "https://linkedin.com/in/jane",
            twitterUrl: "https://x.com/jane",
            gitHubUrl: "https://github.com/jane",
            telegramUrl: "https://t.me/jane");

    private Profile SeedProfile(Guid userId)
    {
        Profile profile = Profile.Create(userId, "Placeholder", null, null, false, true, null);
        _db.Profiles.Add(profile);
        _db.SaveChanges();
        return profile;
    }
}
