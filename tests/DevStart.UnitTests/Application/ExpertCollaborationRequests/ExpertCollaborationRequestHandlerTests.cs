using DevStart.Application.ExpertCollaborationRequests;
using DevStart.Application.ExpertCollaborationRequests.Accept;
using DevStart.Application.ExpertCollaborationRequests.Create;
using DevStart.Application.ExpertCollaborationRequests.GetAllByExpertProfileId;
using DevStart.Application.ExpertCollaborationRequests.GetAllByStartupId;
using DevStart.Application.ExpertCollaborationRequests.GetById;
using DevStart.Application.ExpertCollaborationRequests.Reject;
using DevStart.Application.ExpertCollaborationRequests.Withdraw;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Experts;
using DevStart.Domain.Profiles;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Application.Startups;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.ExpertCollaborationRequests;

public sealed class ExpertCollaborationRequestHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 28, 10, 0, 0, DateTimeKind.Utc);
    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FixedDateTimeProvider _clock = new() { UtcNow = Now };
    private readonly ExpertCollaborationOptions _options = new();

    // ---------- Create: expert applies to a startup ----------

    [Fact]
    public async Task Create_ShouldFail_WhenExpertProfileIsMissing()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        var sut = CreateHandler(expertId);

        Result<Guid> result = await sut.Handle(CreateCommand(startup.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.ExpertProfileRequired);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenStartupIsMissing()
    {
        Guid expertId = Guid.NewGuid();
        SeedExpertProfile(expertId);
        var sut = CreateHandler(expertId);
        Guid missingStartupId = Guid.NewGuid();

        Result<Guid> result = await sut.Handle(CreateCommand(missingStartupId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Startups.NotFound");
    }

    [Fact]
    public async Task Create_ShouldFail_WhenStartupIsBanned()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        startup.Ban("spam", expiresAt: null, byUserId: Guid.NewGuid(), Now);
        await _db.SaveChangesAsync();
        SeedExpertProfile(expertId);
        var sut = CreateHandler(expertId);

        Result<Guid> result = await sut.Handle(CreateCommand(startup.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.StartupUnavailable);
    }

    [Fact]
    public async Task Create_ShouldSucceed_WhenTemporaryBanHasAlreadyLapsed()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        startup.Ban("spam", expiresAt: Now.AddDays(1), byUserId: Guid.NewGuid(), Now);
        await _db.SaveChangesAsync();
        SeedExpertProfile(expertId);
        // The hourly unban job has not run yet, but the ban window is over.
        _clock.UtcNow = Now.AddDays(2);
        var sut = CreateHandler(expertId);

        Result<Guid> result = await sut.Handle(CreateCommand(startup.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_ShouldFail_WhenExpertBelongsToStartup()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId);
        SeedStartupMember(expertId, startup.Id, StartupRole.Member);
        var sut = CreateHandler(expertId);

        Result<Guid> result = await sut.Handle(CreateCommand(startup.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.CannotApplyToOwnStartup);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenPendingRequestAlreadyExists()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId);
        SeedRequest(expertId, startup.Id);
        var sut = CreateHandler(expertId);

        Result<Guid> result = await sut.Handle(CreateCommand(startup.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.AlreadyExistsForStartup);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenStartupAlreadyHasAPendingInvitationForTheSameExpert()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId);
        SeedRequest(expertId, startup.Id, CollaborationRequestInitiator.Startup);
        var sut = CreateHandler(expertId);

        Result<Guid> result = await sut.Handle(CreateCommand(startup.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.AlreadyExistsForStartup);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenClaimingSomeoneElsesExpertProfile()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId);
        var sut = CreateHandler(expertId);

        Result<Guid> result = await sut.Handle(
            CreateCommand(startup.Id, expertProfileId: Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldPersistPendingExpertInitiatedRequest_WhenInputIsValid()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId);
        var sut = CreateHandler(expertId);

        Result<Guid> result = await sut.Handle(CreateCommand(startup.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        ExpertCollaborationRequest request = await _db.ExpertCollaborationRequests
            .SingleAsync(r => r.Id == result.Value);
        request.ExpertProfileId.ShouldBe(expertId);
        request.StartupId.ShouldBe(startup.Id);
        request.Initiator.ShouldBe(CollaborationRequestInitiator.Expert);
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Pending);
        request.CreatedAt.ShouldBe(Now);
        request.UpdatedAt.ShouldBe(Now);
    }

    // ---------- Create: startup invites an expert ----------

    [Fact]
    public async Task Create_ShouldPersistStartupInitiatedRequest_WhenFounderInvitesAnExpert()
    {
        Guid expertId = Guid.NewGuid();
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId);
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        var sut = CreateHandler(founderId);

        Result<Guid> result = await sut.Handle(
            CreateCommand(startup.Id, expertProfileId: expertId),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        ExpertCollaborationRequest request = await _db.ExpertCollaborationRequests
            .SingleAsync(r => r.Id == result.Value);
        request.ExpertProfileId.ShouldBe(expertId);
        request.Initiator.ShouldBe(CollaborationRequestInitiator.Startup);
        request.AwaitsExpertResponse.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_ShouldFail_WhenFounderOmitsTheInvitedExpert()
    {
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        var sut = CreateHandler(founderId);

        Result<Guid> result = await sut.Handle(CreateCommand(startup.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.ExpertProfileIdRequired);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenInvitedUserHasNoExpertProfile()
    {
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        var sut = CreateHandler(founderId);

        Result<Guid> result = await sut.Handle(
            CreateCommand(startup.Id, expertProfileId: Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.ExpertProfileNotFound);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenInvitedExpertIsAlreadyOnTheTeam()
    {
        Guid expertId = Guid.NewGuid();
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId);
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        SeedStartupMember(expertId, startup.Id, StartupRole.Member);
        var sut = CreateHandler(founderId);

        Result<Guid> result = await sut.Handle(
            CreateCommand(startup.Id, expertProfileId: expertId),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.ExpertAlreadyMember);
    }

    // ---------- Create: rejection cooldown ----------

    [Fact]
    public async Task Create_ShouldFail_WhenTheSameSideWasRejectedWithinTheCooldown()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId);
        SeedRejectedRequest(expertId, startup.Id, CollaborationRequestInitiator.Expert, Now.AddDays(-3));
        var sut = CreateHandler(expertId);

        Result<Guid> result = await sut.Handle(CreateCommand(startup.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ExpertCollaborationRequests.RejectionCooldownActive");
    }

    [Fact]
    public async Task Create_ShouldSucceed_WhenTheCooldownHasElapsed()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId);
        SeedRejectedRequest(expertId, startup.Id, CollaborationRequestInitiator.Expert, Now.AddDays(-30));
        var sut = CreateHandler(expertId);

        Result<Guid> result = await sut.Handle(CreateCommand(startup.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_ShouldSucceed_WhenTheRejectingSideChangesItsMind()
    {
        // The startup rejected the expert's application; the startup itself is free to invite them
        // straight away — only the rejected side waits out the cooldown.
        Guid expertId = Guid.NewGuid();
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId);
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        SeedRejectedRequest(expertId, startup.Id, CollaborationRequestInitiator.Expert, Now.AddDays(-1));
        var sut = CreateHandler(founderId);

        Result<Guid> result = await sut.Handle(
            CreateCommand(startup.Id, expertProfileId: expertId),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_ShouldSucceed_WhenCooldownIsDisabled()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId);
        SeedRejectedRequest(expertId, startup.Id, CollaborationRequestInitiator.Expert, Now.AddDays(-1));
        _options.RejectionCooldownDays = 0;
        var sut = CreateHandler(expertId);

        Result<Guid> result = await sut.Handle(CreateCommand(startup.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    // ---------- Accept / Reject ----------

    [Fact]
    public async Task Accept_ShouldSetAccepted_WhenCurrentUserIsFounder()
    {
        Guid expertId = Guid.NewGuid();
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);

        Result result = await AcceptHandler(founderId).Handle(
            new AcceptExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Accepted);
        request.UpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Accept_ShouldFail_WhenCurrentUserIsRegularStartupMember()
    {
        Guid expertId = Guid.NewGuid();
        Guid memberId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedStartupMember(memberId, startup.Id, StartupRole.Member);
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);

        Result result = await AcceptHandler(memberId).Handle(
            new AcceptExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.Unauthorized);
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Pending);
    }

    [Fact]
    public async Task Accept_ShouldSetAccepted_WhenExpertAnswersAStartupInvitation()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id, CollaborationRequestInitiator.Startup);

        Result result = await AcceptHandler(expertId).Handle(
            new AcceptExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Accepted);
    }

    [Fact]
    public async Task Accept_ShouldFail_WhenFounderTriesToAnswerTheirOwnInvitation()
    {
        Guid expertId = Guid.NewGuid();
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id, CollaborationRequestInitiator.Startup);

        Result result = await AcceptHandler(founderId).Handle(
            new AcceptExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.Unauthorized);
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Pending);
    }

    [Fact]
    public async Task Accept_ShouldFail_WhenRequestIsNotPending()
    {
        Guid expertId = Guid.NewGuid();
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);
        request.Reject(Now);
        await _db.SaveChangesAsync();

        Result result = await AcceptHandler(founderId).Handle(
            new AcceptExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.MustBePending);
    }

    [Fact]
    public async Task Reject_ShouldSetRejected_WhenCurrentUserIsAdmin()
    {
        Guid expertId = Guid.NewGuid();
        Guid adminId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedStartupMember(adminId, startup.Id, StartupRole.Administration);
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);
        var sut = new RejectExpertCollaborationRequestCommandHandler(
            _db, new TestUserContext(adminId), new StartupAuthorizationService(_db), _clock);

        Result result = await sut.Handle(new RejectExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Rejected);
        request.UpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Reject_ShouldSetRejected_WhenExpertDeclinesAStartupInvitation()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id, CollaborationRequestInitiator.Startup);
        var sut = new RejectExpertCollaborationRequestCommandHandler(
            _db, new TestUserContext(expertId), new StartupAuthorizationService(_db), _clock);

        Result result = await sut.Handle(new RejectExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Rejected);
    }

    [Fact]
    public async Task Reject_ShouldFail_WhenRequestIsNotPending()
    {
        Guid expertId = Guid.NewGuid();
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);
        request.Accept(Now);
        await _db.SaveChangesAsync();
        var sut = new RejectExpertCollaborationRequestCommandHandler(
            _db, new TestUserContext(founderId), new StartupAuthorizationService(_db), _clock);

        Result result = await sut.Handle(new RejectExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.MustBePending);
    }

    // ---------- Withdraw ----------

    [Fact]
    public async Task Withdraw_ShouldSetWithdrawn_WhenCurrentUserIsRequestExpert()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);

        Result result = await WithdrawHandler(expertId).Handle(
            new WithdrawExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Withdrawn);
        request.UpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Withdraw_ShouldFail_WhenCurrentUserIsNotRequestExpert()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);

        Result result = await WithdrawHandler(Guid.NewGuid()).Handle(
            new WithdrawExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.Unauthorized);
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Pending);
    }

    [Fact]
    public async Task Withdraw_ShouldSucceed_WhenFounderTakesBackTheirInvitation()
    {
        Guid expertId = Guid.NewGuid();
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id, CollaborationRequestInitiator.Startup);

        Result result = await WithdrawHandler(founderId).Handle(
            new WithdrawExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Withdrawn);
    }

    [Fact]
    public async Task Withdraw_ShouldFail_WhenExpertTriesToWithdrawAnInvitationAddressedToThem()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id, CollaborationRequestInitiator.Startup);

        Result result = await WithdrawHandler(expertId).Handle(
            new WithdrawExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.Unauthorized);
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Pending);
    }

    [Fact]
    public async Task Withdraw_ShouldFail_WhenRequestIsNotPending()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);
        request.Accept(Now);
        await _db.SaveChangesAsync();

        Result result = await WithdrawHandler(expertId).Handle(
            new WithdrawExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.MustBePending);
    }

    // ---------- Reads ----------

    [Fact]
    public async Task GetById_ShouldReturnRequest_WhenCurrentUserIsRequestExpert()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId, "Expert Name");
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id, CollaborationRequestInitiator.Startup);
        var sut = new GetExpertCollaborationRequestByIdQueryHandler(
            _db, new TestUserContext(expertId), new StartupAuthorizationService(_db));

        Result<ExpertCollaborationRequestResponse> result = await sut.Handle(
            new GetExpertCollaborationRequestByIdQuery(request.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(request.Id);
        result.Value.ExpertDisplayName.ShouldBe("Expert Name");
        result.Value.StartupName.ShouldBe(startup.Name);
        result.Value.Initiator.ShouldBe(CollaborationRequestInitiator.Startup);
    }

    [Fact]
    public async Task GetById_ShouldFail_WhenCurrentUserIsNotParticipant()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);
        var sut = new GetExpertCollaborationRequestByIdQueryHandler(
            _db, new TestUserContext(Guid.NewGuid()), new StartupAuthorizationService(_db));

        Result<ExpertCollaborationRequestResponse> result = await sut.Handle(
            new GetExpertCollaborationRequestByIdQuery(request.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.Unauthorized);
    }

    [Fact]
    public async Task GetAllByStartupId_ShouldReturnRequests_WhenCurrentUserIsFounder()
    {
        Guid expertId = Guid.NewGuid();
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId, "Expert Name");
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);

        Result<List<ExpertCollaborationRequestResponse>> result = await StartupListHandler(founderId).Handle(
            new GetExpertCollaborationRequestsByStartupIdQuery(startup.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(r => r.Id).ShouldBe([request.Id]);
        result.Value[0].ExpertDisplayName.ShouldBe("Expert Name");
        result.Value[0].StartupName.ShouldBe(startup.Name);
    }

    [Fact]
    public async Task GetAllByStartupId_ShouldKeepRow_WhenExpertHasNoProfileRow()
    {
        Guid expertId = Guid.NewGuid();
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);

        Result<List<ExpertCollaborationRequestResponse>> result = await StartupListHandler(founderId).Handle(
            new GetExpertCollaborationRequestsByStartupIdQuery(startup.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(r => r.Id).ShouldBe([request.Id]);
        result.Value[0].ExpertDisplayName.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllByStartupId_ShouldFilterByStatus()
    {
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        SeedRequest(Guid.NewGuid(), startup.Id);
        ExpertCollaborationRequest accepted = SeedRequest(Guid.NewGuid(), startup.Id);
        accepted.Accept(Now);
        await _db.SaveChangesAsync();

        Result<List<ExpertCollaborationRequestResponse>> result = await StartupListHandler(founderId).Handle(
            new GetExpertCollaborationRequestsByStartupIdQuery(startup.Id, ExpertCollaborationRequestStatus.Accepted),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(r => r.Id).ShouldBe([accepted.Id]);
    }

    [Fact]
    public async Task GetAllByStartupId_ShouldOrderPendingFirstAndPaginate()
    {
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);

        // Newest first, so the answered-but-newest row would lead without the pending-first rule.
        ExpertCollaborationRequest answered = SeedRequest(Guid.NewGuid(), startup.Id, createdAt: Now.AddDays(1));
        answered.Reject(Now);
        ExpertCollaborationRequest pending = SeedRequest(Guid.NewGuid(), startup.Id, createdAt: Now);
        await _db.SaveChangesAsync();

        Result<List<ExpertCollaborationRequestResponse>> firstPage = await StartupListHandler(founderId).Handle(
            new GetExpertCollaborationRequestsByStartupIdQuery(startup.Id, status: null, pageNumber: 1, pageSize: 1),
            CancellationToken.None);
        Result<List<ExpertCollaborationRequestResponse>> secondPage = await StartupListHandler(founderId).Handle(
            new GetExpertCollaborationRequestsByStartupIdQuery(startup.Id, status: null, pageNumber: 2, pageSize: 1),
            CancellationToken.None);

        firstPage.Value.Select(r => r.Id).ShouldBe([pending.Id]);
        secondPage.Value.Select(r => r.Id).ShouldBe([answered.Id]);
    }

    [Fact]
    public async Task GetAllByStartupId_ShouldFail_WhenCurrentUserDoesNotRunTheStartup()
    {
        Startup startup = SeedStartup();
        SeedRequest(Guid.NewGuid(), startup.Id);

        Result<List<ExpertCollaborationRequestResponse>> result = await StartupListHandler(Guid.NewGuid()).Handle(
            new GetExpertCollaborationRequestsByStartupIdQuery(startup.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.Unauthorized);
    }

    [Fact]
    public async Task GetAllByExpertProfileId_ShouldReturnRequests_WhenCurrentUserOwnsExpertProfile()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId, "Expert Name");
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);
        var sut = new GetExpertCollaborationRequestsByExpertProfileIdQueryHandler(_db, new TestUserContext(expertId));

        Result<List<ExpertCollaborationRequestResponse>> result = await sut.Handle(
            new GetExpertCollaborationRequestsByExpertProfileIdQuery(expertId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(r => r.Id).ShouldBe([request.Id]);
        result.Value[0].ExpertDisplayName.ShouldBe("Expert Name");
        result.Value[0].StartupName.ShouldBe(startup.Name);
    }

    [Fact]
    public async Task GetAllByExpertProfileId_ShouldFilterByStatusAndPaginate()
    {
        Guid expertId = Guid.NewGuid();
        SeedExpertProfile(expertId);
        Startup first = SeedStartup("First");
        Startup second = SeedStartup("Second");
        SeedRequest(expertId, first.Id);
        ExpertCollaborationRequest withdrawn = SeedRequest(expertId, second.Id);
        withdrawn.Withdraw(Now);
        await _db.SaveChangesAsync();
        var sut = new GetExpertCollaborationRequestsByExpertProfileIdQueryHandler(_db, new TestUserContext(expertId));

        Result<List<ExpertCollaborationRequestResponse>> result = await sut.Handle(
            new GetExpertCollaborationRequestsByExpertProfileIdQuery(
                expertId, ExpertCollaborationRequestStatus.Withdrawn, pageNumber: 1, pageSize: 10),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(r => r.Id).ShouldBe([withdrawn.Id]);
        result.Value[0].StartupName.ShouldBe("Second");
    }

    [Fact]
    public async Task GetAllByExpertProfileId_ShouldFail_WhenReadingSomeoneElsesRequests()
    {
        Guid expertId = Guid.NewGuid();
        var sut = new GetExpertCollaborationRequestsByExpertProfileIdQueryHandler(_db, new TestUserContext(Guid.NewGuid()));

        Result<List<ExpertCollaborationRequestResponse>> result = await sut.Handle(
            new GetExpertCollaborationRequestsByExpertProfileIdQuery(expertId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.Unauthorized);
    }

    // ---------- Mapping ----------

    [Fact]
    public void EfModel_ShouldDefineUniqueFilteredIndex_ForPendingExpertStartupPair()
    {
        IEntityType entityType = _db.Model.FindEntityType(typeof(ExpertCollaborationRequest))!;

        IIndex index = entityType.GetIndexes().Single(
            i => i.GetDatabaseName() == "ux_expert_collaboration_requests_expert_startup_pending");

        index.IsUnique.ShouldBeTrue();
        index.GetFilter().ShouldBe("status = 0");
        index.Properties.Select(p => p.Name).ShouldBe(["ExpertProfileId", "StartupId"]);
    }

    [Fact]
    public void EfModel_ShouldDefineFilteredIndex_ForPendingAgeScan()
    {
        IEntityType entityType = _db.Model.FindEntityType(typeof(ExpertCollaborationRequest))!;

        IIndex index = entityType.GetIndexes().Single(
            i => i.GetDatabaseName() == "ix_expert_collaboration_requests_pending_created_at");

        index.GetFilter().ShouldBe("status = 0");
        index.Properties.Select(p => p.Name).ShouldBe(["CreatedAt"]);
    }

    [Fact]
    public void EfModel_ShouldNotMapTheDerivedDirectionFlag()
    {
        IEntityType entityType = _db.Model.FindEntityType(typeof(ExpertCollaborationRequest))!;

        entityType.FindProperty(nameof(ExpertCollaborationRequest.AwaitsExpertResponse)).ShouldBeNull();
        entityType.FindProperty(nameof(ExpertCollaborationRequest.Initiator)).ShouldNotBeNull();
    }

    // ---------- Fixtures ----------

    private CreateExpertCollaborationRequestCommandHandler CreateHandler(Guid userId) =>
        new(_db,
            new TestUserContext(userId),
            new StartupAuthorizationService(_db),
            _clock,
            Options.Create(_options));

    private AcceptExpertCollaborationRequestCommandHandler AcceptHandler(Guid userId) =>
        new(_db, new TestUserContext(userId), new StartupAuthorizationService(_db), _clock);

    private WithdrawExpertCollaborationRequestCommandHandler WithdrawHandler(Guid userId) =>
        new(_db, new TestUserContext(userId), new StartupAuthorizationService(_db), _clock);

    private GetExpertCollaborationRequestsByStartupIdQueryHandler StartupListHandler(Guid userId) =>
        new(_db, new TestUserContext(userId), new StartupAuthorizationService(_db));

    private static CreateExpertCollaborationRequestCommand CreateCommand(Guid startupId, Guid? expertProfileId = null) =>
        new(
            startupId,
            expertProfileId,
            CollaborationType.Advisor,
            "I can help with product strategy.",
            proposedHoursPerWeek: 8,
            proposedRate: 100m);

    private ExpertProfile SeedExpertProfile(Guid userId, string displayName = "Expert")
    {
        ExpertProfile expertProfile = ExpertProfile.Create(userId, Now);

        _db.ExpertProfiles.Add(expertProfile);

        Profile profile = Profile.Create(userId, displayName, null, null, false, true, null);
        _db.Profiles.Add(profile);

        _db.SaveChanges();

        return expertProfile;
    }

    private Startup SeedStartup(string name = "Startup")
    {
        Startup startup = Startup.Create(
            name,
            "startup@example.com",
            description: null,
            url: null,
            StartupStage.Mvp,
            StartupLocation.Other,
            billingEmail: null,
            avatarId: null,
            Now,
            socialMediaLinks: [],
            shortDescription: null);

        _db.Startups.Add(startup);
        _db.SaveChanges();

        return startup;
    }

    private StartupMember SeedStartupMember(Guid userId, Guid startupId, StartupRole role)
    {
        StartupMember member = StartupMember.Create(
            userId,
            startupId,
            role,
            isPublic: true,
            Now);

        _db.StartupMembers.Add(member);
        _db.SaveChanges();

        return member;
    }

    private ExpertCollaborationRequest SeedRequest(
        Guid expertId,
        Guid startupId,
        CollaborationRequestInitiator initiator = CollaborationRequestInitiator.Expert,
        DateTime? createdAt = null)
    {
        ExpertCollaborationRequest request = ExpertCollaborationRequest.Create(
            expertId,
            startupId,
            initiator,
            CollaborationType.Consultant,
            message: null,
            proposedHoursPerWeek: null,
            proposedRate: null,
            createdAt ?? Now);

        _db.ExpertCollaborationRequests.Add(request);
        _db.SaveChanges();

        return request;
    }

    private void SeedRejectedRequest(
        Guid expertId,
        Guid startupId,
        CollaborationRequestInitiator initiator,
        DateTime rejectedAt)
    {
        ExpertCollaborationRequest request = SeedRequest(expertId, startupId, initiator, rejectedAt.AddDays(-1));
        request.Reject(rejectedAt);
        _db.SaveChanges();
    }
}
