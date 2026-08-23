using DevStart.Application.InvestorProfiles.Create;
using DevStart.Application.InvestorProfiles.GetById;
using DevStart.Application.InvestorProfiles.Update;
using DevStart.Domain.Investors;
using DevStart.Domain.Profiles;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.UnitTests.Application.InvestorProfiles;

/// <summary>
/// The investor dashboard edits one form but two entities: the type lives on InvestorProfile,
/// everything personal on the shared Profile. These assert the write path stores what the read path
/// hands back — the two used to disagree, and the personal fields were dropped without an error.
/// </summary>
public sealed class InvestorProfileHandlerTests
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
        profile.Name.ShouldBe("Jane Investor");
        profile.Bio.ShouldBe("Инвестирую в early stage");
        profile.Url.ShouldBe("https://jane.fund");
        profile.IsPublic.ShouldBeFalse();
    }

    [Fact]
    public async Task Create_ShouldRoundTripThroughTheReadModel()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);

        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        // Профиль создаётся непубличным, поэтому читаем его как владелец — так же, как дашборд.
        Result<InvestorProfileResponse> read = await new GetInvestorProfileByIdQueryHandler(_db)
            .Handle(new GetInvestorProfileByIdQuery(userId, userId), CancellationToken.None);

        read.IsSuccess.ShouldBeTrue();
        read.Value.DisplayName.ShouldBe("Jane Investor");
        read.Value.Bio.ShouldBe("Инвестирую в early stage");
        read.Value.Website.ShouldBe("https://jane.fund");
        read.Value.IsPublic.ShouldBeFalse();
        read.Value.Type.ShouldBe(InvestorProfileType.Fund);
    }

    [Fact]
    public async Task Create_ShouldLeaveTheFieldsTheInvestorFormDoesNotEdit()
    {
        // The same user may also hold an expert profile: its social links, plus the settings-page
        // fields, must survive an investor save.
        Guid userId = Guid.NewGuid();
        Guid avatarId = Guid.NewGuid();
        Profile seeded = SeedProfile(userId);
        seeded.AvatarId = avatarId;
        seeded.IsAvailableForHire = true;
        seeded.SocialMediaLinks = ["https://vc.ru/jane"];
        seeded.LinkedInUrl = "https://linkedin.com/in/jane";
        seeded.TelegramUrl = "https://t.me/jane";
        await _db.SaveChangesAsync();

        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        Profile profile = await _db.Profiles.SingleAsync(p => p.UserId == userId);
        profile.AvatarId.ShouldBe(avatarId);
        profile.IsAvailableForHire.ShouldBeTrue();
        profile.SocialMediaLinks.ShouldBe(["https://vc.ru/jane"]);
        profile.LinkedInUrl.ShouldBe("https://linkedin.com/in/jane");
        profile.TelegramUrl.ShouldBe("https://t.me/jane");
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
    public async Task Create_ShouldFail_WhenAnInvestorProfileAlreadyExists()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);
        _db.InvestorProfiles.Add(InvestorProfile.Create(userId, InvestorProfileType.Individual, Now));
        await _db.SaveChangesAsync();

        Result<Guid> result = await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(InvestorProfileErrors.AlreadyExists(userId));
    }

    [Fact]
    public async Task Update_ShouldOverwriteThePersonalFieldsAndType()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);
        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        Result result = await UpdateHandler(userId).Handle(
            new UpdateInvestorProfileCommand(
                InvestorProfileType.Individual,
                displayName: "Jane I.",
                bio: "Обновлённое био",
                website: "https://jane.example",
                isPublic: true),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        Profile profile = await _db.Profiles.SingleAsync(p => p.UserId == userId);
        profile.Name.ShouldBe("Jane I.");
        profile.Bio.ShouldBe("Обновлённое био");
        profile.Url.ShouldBe("https://jane.example");
        profile.IsPublic.ShouldBeTrue();

        InvestorProfile investorProfile = await _db.InvestorProfiles.SingleAsync(ip => ip.UserId == userId);
        investorProfile.Type.ShouldBe(InvestorProfileType.Individual);
    }

    [Fact]
    public async Task Update_ShouldStoreBlankOptionalFieldsAsNull()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);
        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        await UpdateHandler(userId).Handle(
            new UpdateInvestorProfileCommand(
                InvestorProfileType.Fund,
                displayName: "  Jane Investor  ",
                bio: "   ",
                website: "",
                isPublic: true),
            CancellationToken.None);

        Profile profile = await _db.Profiles.SingleAsync(p => p.UserId == userId);
        profile.Name.ShouldBe("Jane Investor");
        profile.Bio.ShouldBeNull();
        profile.Url.ShouldBeNull();
    }

    [Fact]
    public async Task Update_ShouldStoreTheFundLogoWithoutTouchingThePersonalAvatar()
    {
        Guid userId = Guid.NewGuid();
        Guid personalAvatarId = Guid.NewGuid();
        Guid fundAvatarId = Guid.NewGuid();
        Profile profile = SeedProfile(userId);
        profile.AvatarId = personalAvatarId;
        await _db.SaveChangesAsync();
        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        await UpdateHandler(userId).Handle(
            new UpdateInvestorProfileCommand(
                InvestorProfileType.Fund,
                displayName: "Jane Fund",
                isPublic: true,
                avatarId: fundAvatarId),
            CancellationToken.None);

        InvestorProfile investorProfile = await _db.InvestorProfiles.SingleAsync(ip => ip.UserId == userId);
        investorProfile.AvatarId.ShouldBe(fundAvatarId);

        // Личная аватарка аккаунта остаётся своей: логотип фонда её не подменяет.
        Profile stored = await _db.Profiles.SingleAsync(p => p.UserId == userId);
        stored.AvatarId.ShouldBe(personalAvatarId);
    }

    [Fact]
    public async Task Update_ShouldClearTheFundLogo_WhenTheTypeSwitchesToIndividual()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);
        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);
        await UpdateHandler(userId).Handle(
            new UpdateInvestorProfileCommand(
                InvestorProfileType.Fund,
                displayName: "Jane Fund",
                isPublic: true,
                avatarId: Guid.NewGuid()),
            CancellationToken.None);

        await UpdateHandler(userId).Handle(
            new UpdateInvestorProfileCommand(
                InvestorProfileType.Individual,
                displayName: "Jane I.",
                isPublic: true,
                avatarId: Guid.NewGuid()),
            CancellationToken.None);

        InvestorProfile investorProfile = await _db.InvestorProfiles.SingleAsync(ip => ip.UserId == userId);
        investorProfile.AvatarId.ShouldBeNull();
    }

    [Fact]
    public async Task Update_ShouldFail_WhenNoInvestorProfileExists()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);

        Result result = await UpdateHandler(userId).Handle(
            new UpdateInvestorProfileCommand(InvestorProfileType.Fund, displayName: "Jane"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(InvestorProfileErrors.NotFound(userId));
    }

    /// <summary>
    /// Каталог инвесторов открыт анониму, а карточка — нет: клик по видимому в списке профилю
    /// отвечал «инвестора не существует». Читатель карточки теперь тот же, что и читатель каталога.
    /// </summary>
    [Fact]
    public async Task GetById_ShouldReturnAPublicProfile_ToAnAnonymousViewer()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);
        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);
        await UpdateHandler(userId).Handle(
            new UpdateInvestorProfileCommand(InvestorProfileType.Fund, displayName: "Jane Fund", isPublic: true),
            CancellationToken.None);

        Result<InvestorProfileResponse> read = await new GetInvestorProfileByIdQueryHandler(_db)
            .Handle(new GetInvestorProfileByIdQuery(userId, viewerId: null), CancellationToken.None);

        read.IsSuccess.ShouldBeTrue();
        read.Value.DisplayName.ShouldBe("Jane Fund");
    }

    [Fact]
    public async Task GetById_ShouldHideANonPublicProfile_FromEveryoneButItsOwner()
    {
        Guid userId = Guid.NewGuid();
        SeedProfile(userId);
        await CreateHandler(userId).Handle(CreateCommand(), CancellationToken.None);

        var handler = new GetInvestorProfileByIdQueryHandler(_db);

        Result<InvestorProfileResponse> anonymous = await handler
            .Handle(new GetInvestorProfileByIdQuery(userId, viewerId: null), CancellationToken.None);
        Result<InvestorProfileResponse> stranger = await handler
            .Handle(new GetInvestorProfileByIdQuery(userId, Guid.NewGuid()), CancellationToken.None);
        Result<InvestorProfileResponse> owner = await handler
            .Handle(new GetInvestorProfileByIdQuery(userId, userId), CancellationToken.None);

        // Не «нет доступа», а «нет такого»: каталог непубличный профиль не показывает, значит и по
        // прямой ссылке подтверждать его существование нечем.
        anonymous.Error.ShouldBe(InvestorProfileErrors.NotFound(userId));
        stranger.Error.ShouldBe(InvestorProfileErrors.NotFound(userId));
        owner.IsSuccess.ShouldBeTrue();
    }

    private CreateInvestorProfileCommandHandler CreateHandler(Guid userId) =>
        new(_db, new TestUserContext(userId), _clock);

    private UpdateInvestorProfileCommandHandler UpdateHandler(Guid userId) =>
        new(_db, new TestUserContext(userId), _clock);

    private static CreateInvestorProfileCommand CreateCommand() =>
        new(InvestorProfileType.Fund,
            displayName: "Jane Investor",
            bio: "Инвестирую в early stage",
            website: "https://jane.fund",
            isPublic: false);

    private Profile SeedProfile(Guid userId)
    {
        Profile profile = Profile.Create(userId, "Placeholder", null, null, false, true, null);
        _db.Profiles.Add(profile);
        _db.SaveChanges();
        return profile;
    }
}
