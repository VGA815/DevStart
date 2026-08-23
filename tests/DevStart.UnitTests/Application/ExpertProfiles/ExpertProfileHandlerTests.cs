using DevStart.Application.ExpertExperiences.GetAllByExpertProfileId;
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

        // Профиль создаётся непубличным, поэтому читаем его как владелец — так же, как дашборд.
        Result<ExpertProfileResponse> read = await new GetExpertProfileByIdQueryHandler(_db)
            .Handle(new GetExpertProfileByIdQuery(userId, userId), CancellationToken.None);

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

    /// <summary>
    /// Каталог экспертов открыт анониму, а карточка — нет: клик по видимому в списке профилю
    /// отвечал «эксперта не существует». Читатель карточки теперь тот же, что и читатель каталога.
    /// </summary>
    [Fact]
    public async Task GetById_ShouldReturnAPublicProfile_ToAnAnonymousViewer()
    {
        Guid userId = Guid.NewGuid();
        await SeedPublicExpert(userId);

        Result<ExpertProfileResponse> read = await new GetExpertProfileByIdQueryHandler(_db)
            .Handle(new GetExpertProfileByIdQuery(userId, viewerId: null), CancellationToken.None);

        read.IsSuccess.ShouldBeTrue();
        read.Value.DisplayName.ShouldBe("Jane Public");
    }

    [Fact]
    public async Task GetById_ShouldHideANonPublicProfile_FromEveryoneButItsOwner()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);
        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        var handler = new GetExpertProfileByIdQueryHandler(_db);

        Result<ExpertProfileResponse> anonymous = await handler
            .Handle(new GetExpertProfileByIdQuery(userId, viewerId: null), CancellationToken.None);
        Result<ExpertProfileResponse> stranger = await handler
            .Handle(new GetExpertProfileByIdQuery(userId, Guid.NewGuid()), CancellationToken.None);
        Result<ExpertProfileResponse> owner = await handler
            .Handle(new GetExpertProfileByIdQuery(userId, userId), CancellationToken.None);

        // Не «нет доступа», а «нет такого»: каталог непубличный профиль не показывает, значит и по
        // прямой ссылке подтверждать его существование нечем.
        anonymous.Error.ShouldBe(ExpertProfileErrors.NotFound(userId));
        stranger.Error.ShouldBe(ExpertProfileErrors.NotFound(userId));
        owner.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// Опыт — основное содержимое карточки. Если он остаётся закрытым, аноним получает открывшуюся,
    /// но пустую страницу — то же самое «профиля как будто нет», только другими словами.
    /// </summary>
    [Fact]
    public async Task GetExperiences_ShouldFollowTheVisibilityOfTheCardTheyBelongTo()
    {
        Guid publicUserId = Guid.NewGuid();
        Guid privateUserId = Guid.NewGuid();
        Guid publicProfileId = await SeedPublicExpert(publicUserId, "Jane Public");

        SeedProfile(privateUserId);
        await CreateHandler(privateUserId).Handle(CreateCommand(), CancellationToken.None);
        Guid privateProfileId = (await _db.ExpertProfiles.SingleAsync(ep => ep.UserId == privateUserId)).Id;

        SeedExperience(publicProfileId);
        SeedExperience(privateProfileId);

        var handler = new GetExpertExperiencesByExpertProfileIdQueryHandler(_db);

        Result<List<ExpertExperienceResponse>> onPublic = await handler.Handle(
            new GetExpertExperiencesByExpertProfileIdQuery(publicProfileId, viewerId: null),
            CancellationToken.None);
        Result<List<ExpertExperienceResponse>> onPrivate = await handler.Handle(
            new GetExpertExperiencesByExpertProfileIdQuery(privateProfileId, viewerId: null),
            CancellationToken.None);
        Result<List<ExpertExperienceResponse>> onPrivateAsOwner = await handler.Handle(
            new GetExpertExperiencesByExpertProfileIdQuery(privateProfileId, privateUserId),
            CancellationToken.None);

        onPublic.Value.Count.ShouldBe(1);
        onPrivate.Value.ShouldBeEmpty();
        onPrivateAsOwner.Value.Count.ShouldBe(1);
    }

    private async Task<Guid> SeedPublicExpert(Guid userId, string displayName = "Jane Public")
    {
        SeedProfile(userId);
        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);
        await UpdateHandler(userId).Handle(
            new UpdateExpertProfileCommand(
                [ExpertSpecialization.Product],
                displayName: displayName,
                isPublic: true),
            CancellationToken.None);

        return (await _db.ExpertProfiles.SingleAsync(ep => ep.UserId == userId)).Id;
    }

    private void SeedExperience(Guid expertProfileId)
    {
        _db.ExpertExperiences.Add(ExpertExperience.Create(
            expertProfileId, "Acme", "CPO", new DateOnly(2020, 1, 1), null, null, Now));
        _db.SaveChanges();
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
