using DevStart.Application.AccountDeletion;
using DevStart.Domain.AccountDeletion;
using DevStart.Domain.Admin;
using DevStart.Domain.ChatFiles;
using DevStart.Domain.EmailVerificationTokens;
using DevStart.Domain.Experts;
using DevStart.Domain.ExternalLogins;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.Investors;
using DevStart.Domain.MediaFiles;
using DevStart.Domain.Messages;
using DevStart.Domain.Notifications;
using DevStart.Domain.PasswordResetTokens;
using DevStart.Domain.Payments;
using DevStart.Domain.Profiles;
using DevStart.Domain.RefreshTokens;
using DevStart.Domain.Security;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.StartupCommunityStandards;
using DevStart.Domain.StartupDocumentFiles;
using DevStart.Domain.StartupEquity;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupProducts;
using DevStart.Domain.Startups;
using DevStart.Domain.Subscriptions;
using DevStart.Domain.TrustedDevices;
using DevStart.Domain.TwoFactor;
using DevStart.Domain.UserConsents;
using DevStart.Domain.UserPreferences;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace DevStart.UnitTests.Application.AccountDeletion;

public sealed class AccountEraserTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FixedDateTimeProvider _clock = new() { UtcNow = Now };
    private readonly CapturingFileStorage _storage = new();
    private readonly RecordingCacheService _cache = new();
    private readonly AccountEraser _sut;

    public AccountEraserTests()
    {
        _sut = new AccountEraser(_db, _storage, _cache, _clock, NullLogger<AccountEraser>.Instance);
    }

    private User NewUser(string email = "leaver@example.com")
    {
        User user = User.Create("leaver", email, "hash", Now);
        _db.Users.Add(user);
        _db.Profiles.Add(Profile.Create(user.Id, "Leaver", "bio", null, false, true, null));
        return user;
    }

    private Startup NewStartup(string name, params (Guid ProfileId, StartupRole Role)[] members)
    {
        Startup startup = Startup.Create(
            name, $"{name}@example.com", null, null, StartupStage.Idea, null, null, null, Now, null, null);

        _db.Startups.Add(startup);

        foreach ((Guid profileId, StartupRole role) in members)
        {
            _db.StartupMembers.Add(StartupMember.Create(profileId, startup.Id, role, true, Now));
        }

        return startup;
    }

    [Fact]
    public async Task Erase_RemovesTheAccountAndEverythingIdentifyingAboutIt()
    {
        User user = NewUser();
        _db.RefreshTokens.Add(RefreshToken.Create(user.Id, "token-hash", Now, TimeSpan.FromDays(30), "1.1.1.1", "ua"));
        _db.TrustedDevices.Add(TrustedDevice.Create(
            user.Id, "device-hash", Now, TimeSpan.FromDays(30), "1.1.1.1", "ua", "Chrome"));
        _db.UserTwoFactors.Add(UserTwoFactor.CreatePending(user.Id, "secret", Now));
        _db.TwoFactorRecoveryCodes.Add(TwoFactorRecoveryCode.Create(user.Id, "code-hash", Now));
        _db.UserSecuritySettings.Add(UserSecuritySettings.CreateDefault(user.Id, Now));
        _db.ExternalLogins.Add(ExternalLogin.Create(user.Id, ExternalLoginProvider.Google, "google-1", user.Email, Now));
        _db.UserConsents.Add(UserConsent.Create(user.Id, ConsentType.PrivacyPolicy, "1.0", Now));
        _db.Preferences.Add(UserPreference.Create(user.Id, UserPreferenceTheme.System));
        _db.Notifications.Add(Notification.Create(user.Id, NotificationType.Welcome, "hi", "body", Now));
        _db.EmailVerificationTokens.Add(EmailVerificationToken.Create(user.Id, Now, Now.AddDays(1)));
        _db.PasswordResetTokens.Add(PasswordResetToken.Create(user.Id, Now, Now.AddHours(1)));
        _db.ExpertProfiles.Add(ExpertProfile.Create(user.Id, Now));
        _db.InvestorProfiles.Add(InvestorProfile.Create(user.Id, InvestorProfileType.Individual, Now));
        await _db.SaveChangesAsync();

        Result result = await _sut.EraseAsync(user.Id, default);

        result.IsSuccess.ShouldBeTrue();

        (await _db.Users.AnyAsync(u => u.Id == user.Id)).ShouldBeFalse();
        (await _db.Profiles.AnyAsync(p => p.UserId == user.Id)).ShouldBeFalse();
        (await _db.RefreshTokens.AnyAsync(t => t.UserId == user.Id)).ShouldBeFalse();
        (await _db.TrustedDevices.AnyAsync(d => d.UserId == user.Id)).ShouldBeFalse();
        (await _db.UserTwoFactors.AnyAsync(t => t.UserId == user.Id)).ShouldBeFalse();
        (await _db.TwoFactorRecoveryCodes.AnyAsync(c => c.UserId == user.Id)).ShouldBeFalse();
        (await _db.UserSecuritySettings.AnyAsync(s => s.UserId == user.Id)).ShouldBeFalse();
        (await _db.ExternalLogins.AnyAsync(l => l.UserId == user.Id)).ShouldBeFalse();
        (await _db.UserConsents.AnyAsync(c => c.UserId == user.Id)).ShouldBeFalse();
        (await _db.Preferences.AnyAsync(p => p.UserId == user.Id)).ShouldBeFalse();
        (await _db.Notifications.AnyAsync(n => n.UserId == user.Id)).ShouldBeFalse();
        (await _db.EmailVerificationTokens.AnyAsync(t => t.UserId == user.Id)).ShouldBeFalse();
        (await _db.PasswordResetTokens.AnyAsync(t => t.UserId == user.Id)).ShouldBeFalse();
        (await _db.ExpertProfiles.AnyAsync(p => p.UserId == user.Id)).ShouldBeFalse();
        (await _db.InvestorProfiles.AnyAsync(p => p.UserId == user.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task Erase_KeepsPaymentRecords_BecauseThePolicyCommitsToHoldingThem()
    {
        User user = NewUser();
        Subscription subscription = Subscription.CreatePending(user.Id, SubscriptionPlan.Pro, Now);
        _db.Subscriptions.Add(subscription);
        _db.Payments.Add(Payment.CreatePending(
            user.Id, subscription.Id, PaymentProvider.YooKassa, 990m, "RUB", Now));
        _db.ServiceOrders.Add(ServiceOrder.CreatePending(
            user.Id, ServiceType.ScoringReport, Guid.NewGuid(), 490m, "RUB", Now));
        await _db.SaveChangesAsync();

        await _sut.EraseAsync(user.Id, default);

        // The rows stay, with their amounts and dates. What made them personal — the user and profile
        // they pointed at — is gone, so the remaining user_id identifies nobody.
        (await _db.Payments.CountAsync(p => p.UserId == user.Id)).ShouldBe(1);
        (await _db.Subscriptions.CountAsync(s => s.UserId == user.Id)).ShouldBe(1);
        (await _db.ServiceOrders.CountAsync(o => o.UserId == user.Id)).ShouldBe(1);
        (await _db.Users.AnyAsync(u => u.Id == user.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task Erase_DeletesStartupsWhereTheUserWasTheOnlyFounder()
    {
        User user = NewUser();
        Startup solo = NewStartup("solo", (user.Id, StartupRole.Founder));
        _db.StartupProducts.Add(StartupProduct.Create(
            solo.Id, "problem", "solution", null, null, null));
        _db.StartupDocumentFiles.Add(StartupDocumentFile.Create(
            Guid.NewGuid(), solo.Id, user.Id, "docs/pitch.pdf", "documents",
            StartupDocumentType.Other, 10, "pitch.pdf", Now));
        _db.StartupCommunityDocuments.Add(StartupCommunityDocument.Create(
            solo.Id, CommunityDocumentType.CodeOfConduct, "CoC", "text", user.Id, Now));
        await _db.SaveChangesAsync();

        await _sut.EraseAsync(user.Id, default);

        (await _db.Startups.AnyAsync(s => s.Id == solo.Id)).ShouldBeFalse();
        (await _db.StartupMembers.AnyAsync(m => m.StartupId == solo.Id)).ShouldBeFalse();
        (await _db.StartupProducts.AnyAsync(p => p.StartupId == solo.Id)).ShouldBeFalse();
        (await _db.StartupDocumentFiles.AnyAsync(d => d.StartupId == solo.Id)).ShouldBeFalse();
        (await _db.StartupCommunityDocuments.AnyAsync(d => d.StartupId == solo.Id)).ShouldBeFalse();

        _storage.Deletes.ShouldContain(d => d.ObjectKey == "docs/pitch.pdf" && d.Bucket == "documents");
    }

    [Fact]
    public async Task Erase_KeepsCoFoundedStartups_ButRemovesThePersonFromThem()
    {
        User user = NewUser();
        User coFounder = User.Create("co", "co@example.com", "hash", Now);
        _db.Users.Add(coFounder);
        _db.Profiles.Add(Profile.Create(coFounder.Id, "Co", null, null, false, true, null));

        Startup shared = NewStartup("shared", (user.Id, StartupRole.Founder), (coFounder.Id, StartupRole.Founder));
        _db.StartupEquityHolders.Add(StartupEquityHolder.Create(
            shared.Id, EquityHolderType.Founder, user.Id, null, 50m, null, null, null, Now));
        _db.StartupDocumentFiles.Add(StartupDocumentFile.Create(
            Guid.NewGuid(), shared.Id, user.Id, "docs/deck.pdf", "documents",
            StartupDocumentType.Other, 10, "deck.pdf", Now));
        _db.StartupCommunityDocuments.Add(StartupCommunityDocument.Create(
            shared.Id, CommunityDocumentType.CodeOfConduct, "CoC", "text", user.Id, Now));
        await _db.SaveChangesAsync();

        await _sut.EraseAsync(user.Id, default);

        (await _db.Startups.AnyAsync(s => s.Id == shared.Id)).ShouldBeTrue();
        (await _db.StartupMembers.AnyAsync(m => m.StartupId == shared.Id && m.ProfileId == user.Id)).ShouldBeFalse();
        (await _db.StartupMembers.AnyAsync(m => m.StartupId == shared.Id && m.ProfileId == coFounder.Id)).ShouldBeTrue();

        // The cap table has to keep summing to 100%, so the share survives without its holder.
        StartupEquityHolder holder = await _db.StartupEquityHolders.SingleAsync(h => h.StartupId == shared.Id);
        holder.ProfileId.ShouldBeNull();
        holder.EquityPercentage.ShouldBe(50m);
        holder.Name.ShouldNotBeNullOrWhiteSpace();

        StartupDocumentFile document = await _db.StartupDocumentFiles.SingleAsync(d => d.StartupId == shared.Id);
        document.UploaderId.ShouldBe(Guid.Empty);
        _storage.Deletes.ShouldNotContain(d => d.ObjectKey == "docs/deck.pdf");

        StartupCommunityDocument communityDocument =
            await _db.StartupCommunityDocuments.SingleAsync(d => d.StartupId == shared.Id);
        communityDocument.AuthorId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task Erase_DropsPersonalMessages_ButKeepsWhatWasSentOnAStartupsBehalf()
    {
        User user = NewUser();
        User other = User.Create("other", "other@example.com", "hash", Now);
        _db.Users.Add(other);
        _db.Profiles.Add(Profile.Create(other.Id, "Other", null, null, false, true, null));

        User coFounder = User.Create("co", "co@example.com", "hash", Now);
        _db.Users.Add(coFounder);
        _db.Profiles.Add(Profile.Create(coFounder.Id, "Co", null, null, false, true, null));

        Startup shared = NewStartup("shared", (user.Id, StartupRole.Founder), (coFounder.Id, StartupRole.Founder));

        Message personal = Message.Create(
            user.Id, ChatParticipantType.User, null, other.Id, ChatParticipantType.User,
            "личное сообщение", null, null, null, null, Now);
        Message asStartup = Message.Create(
            shared.Id, ChatParticipantType.Startup, user.Id, other.Id, ChatParticipantType.User,
            "от лица стартапа", null, null, null, null, Now);
        _db.Messages.AddRange(personal, asStartup);

        ChatFile personalAttachment = ChatFile.Create(
            Guid.NewGuid(), user.Id, "chat/personal.pdf", "chat-files", "personal.pdf", "application/pdf", 10, Now);
        personalAttachment.AttachTo(personal.Id);

        ChatFile startupAttachment = ChatFile.Create(
            Guid.NewGuid(), user.Id, "chat/startup.pdf", "chat-files", "startup.pdf", "application/pdf", 10, Now);
        startupAttachment.AttachTo(asStartup.Id);

        _db.ChatFiles.AddRange(personalAttachment, startupAttachment);
        await _db.SaveChangesAsync();

        await _sut.EraseAsync(user.Id, default);

        (await _db.Messages.AnyAsync(m => m.Id == personal.Id)).ShouldBeFalse();
        _storage.Deletes.ShouldContain(d => d.ObjectKey == "chat/personal.pdf");

        Message survivor = await _db.Messages.SingleAsync(m => m.Id == asStartup.Id);
        survivor.TextContent.ShouldBe("от лица стартапа");
        survivor.SentByProfileId.ShouldBeNull();

        ChatFile survivingFile = await _db.ChatFiles.SingleAsync(f => f.MessageId == asStartup.Id);
        survivingFile.UploaderId.ShouldBe(Guid.Empty);
        _storage.Deletes.ShouldNotContain(d => d.ObjectKey == "chat/startup.pdf");
    }

    [Fact]
    public async Task Erase_DeletesTheAvatarFromStorage()
    {
        User user = NewUser();
        MediaFile avatar = MediaFile.Create(user.Id, "avatars/me.png", "media", MediaFileType.Img, 10, Now);
        _db.MediaFiles.Add(avatar);
        await _db.SaveChangesAsync();

        Profile profile = await _db.Profiles.SingleAsync(p => p.UserId == user.Id);
        profile.AvatarId = avatar.Id;
        await _db.SaveChangesAsync();

        await _sut.EraseAsync(user.Id, default);

        (await _db.MediaFiles.AnyAsync(f => f.Id == avatar.Id)).ShouldBeFalse();
        _storage.Deletes.ShouldContain(d => d.ObjectKey == "avatars/me.png" && d.Bucket == "media");
    }

    [Fact]
    public async Task Erase_DeletesInvestmentApplicationsFiledByTheUser()
    {
        User user = NewUser();
        Startup target = NewStartup("target");
        _db.InvestmentApplications.Add(InvestmentApplication.Create(
            user.Id, target.Id, null, 1_000_000m, "давайте поговорим", Now));
        await _db.SaveChangesAsync();

        await _sut.EraseAsync(user.Id, default);

        (await _db.InvestmentApplications.AnyAsync(a => a.InvestorProfileId == user.Id)).ShouldBeFalse();
        (await _db.Startups.AnyAsync(s => s.Id == target.Id)).ShouldBeTrue();
    }

    [Fact]
    public async Task Erase_OfABannedAccount_LeavesTheBanBehindAsAHash()
    {
        User user = NewUser("banned@example.com");
        await _db.SaveChangesAsync();
        user.Ban("abuse", expiresAt: null, byUserId: Guid.NewGuid(), utcNow: Now);
        await _db.SaveChangesAsync();

        await _sut.EraseAsync(user.Id, default);

        BannedIdentity tombstone = await _db.BannedIdentities.SingleAsync();
        tombstone.EmailHash.ShouldBe(BannedIdentity.HashEmail("banned@example.com"));
        tombstone.BanExpiresAt.ShouldBeNull();
        tombstone.IsInForce(Now.AddYears(5)).ShouldBeTrue();
    }

    [Fact]
    public async Task Erase_OfAnAccountWhoseBanAlreadyLapsed_LeavesNothingBehind()
    {
        User user = NewUser();
        await _db.SaveChangesAsync();
        user.Ban("abuse", expiresAt: Now.AddDays(1), byUserId: Guid.NewGuid(), utcNow: Now);
        await _db.SaveChangesAsync();

        _clock.UtcNow = Now.AddDays(2);

        await _sut.EraseAsync(user.Id, default);

        (await _db.BannedIdentities.AnyAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task Erase_ClosesOutThePendingRequest_AndTheRowOutlivesTheUser()
    {
        User user = NewUser();
        AccountDeletionRequest request = AccountDeletionRequest.Create(user.Id, Now, TimeSpan.FromDays(7));
        _db.AccountDeletionRequests.Add(request);
        await _db.SaveChangesAsync();

        _clock.UtcNow = Now.AddDays(7);

        await _sut.EraseAsync(user.Id, default);

        AccountDeletionRequest completed = await _db.AccountDeletionRequests.SingleAsync(r => r.Id == request.Id);
        completed.Status.ShouldBe(AccountDeletionRequestStatus.Completed);
        completed.CompletedAt.ShouldBe(Now.AddDays(7));
        (completed.CompletedAt!.Value - completed.RequestedAt).TotalDays
            .ShouldBeLessThan(AccountDeletionOptions.PromisedMaxDays);
    }

    [Fact]
    public async Task Erase_OfAnAccountThatIsAlreadyGone_Succeeds()
    {
        Result result = await _sut.EraseAsync(Guid.NewGuid(), default);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Erase_SurvivesAStorageOutage_BecauseTheRowsAreWhatTheLawIsAbout()
    {
        User user = NewUser();
        MediaFile avatar = MediaFile.Create(user.Id, "avatars/me.png", "media", MediaFileType.Img, 10, Now);
        _db.MediaFiles.Add(avatar);
        await _db.SaveChangesAsync();
        Profile profile = await _db.Profiles.SingleAsync(p => p.UserId == user.Id);
        profile.AvatarId = avatar.Id;
        await _db.SaveChangesAsync();

        _storage.DeleteException = new InvalidOperationException("MinIO is down");

        Result result = await _sut.EraseAsync(user.Id, default);

        result.IsSuccess.ShouldBeTrue();
        (await _db.Users.AnyAsync(u => u.Id == user.Id)).ShouldBeFalse();
    }
}
