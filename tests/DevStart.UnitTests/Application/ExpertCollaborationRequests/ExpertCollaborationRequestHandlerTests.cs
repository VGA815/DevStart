using DevStart.Application.ExpertCollaborationRequests.Accept;
using DevStart.Application.ExpertCollaborationRequests.Create;
using DevStart.Application.ExpertCollaborationRequests.GetAllByExpertProfileId;
using DevStart.Application.ExpertCollaborationRequests.GetAllByStartupId;
using DevStart.Application.ExpertCollaborationRequests.GetById;
using DevStart.Application.ExpertCollaborationRequests.Reject;
using DevStart.Application.ExpertCollaborationRequests.Withdraw;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Experts;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;

namespace DevStart.UnitTests.Application.ExpertCollaborationRequests;

public sealed class ExpertCollaborationRequestHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 28, 10, 0, 0, DateTimeKind.Utc);
    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FixedDateTimeProvider _clock = new() { UtcNow = Now };

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
    public async Task Create_ShouldPersistPendingRequest_WhenInputIsValid()
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
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Pending);
        request.CreatedAt.ShouldBe(Now);
        request.UpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Accept_ShouldSetAccepted_WhenCurrentUserIsFounder()
    {
        Guid expertId = Guid.NewGuid();
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);
        var sut = new AcceptExpertCollaborationRequestCommandHandler(
            _db,
            new TestUserContext(founderId),
            _clock);

        Result result = await sut.Handle(new AcceptExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

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
        var sut = new AcceptExpertCollaborationRequestCommandHandler(
            _db,
            new TestUserContext(memberId),
            _clock);

        Result result = await sut.Handle(new AcceptExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

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
        var sut = new AcceptExpertCollaborationRequestCommandHandler(
            _db,
            new TestUserContext(founderId),
            _clock);

        Result result = await sut.Handle(new AcceptExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

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
            _db,
            new TestUserContext(adminId),
            _clock);

        Result result = await sut.Handle(new RejectExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Rejected);
        request.UpdatedAt.ShouldBe(Now);
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
            _db,
            new TestUserContext(founderId),
            _clock);

        Result result = await sut.Handle(new RejectExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.MustBePending);
    }

    [Fact]
    public async Task Withdraw_ShouldSetWithdrawn_WhenCurrentUserIsRequestExpert()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);
        var sut = new WithdrawExpertCollaborationRequestCommandHandler(
            _db,
            new TestUserContext(expertId),
            _clock);

        Result result = await sut.Handle(new WithdrawExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Withdrawn);
        request.UpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Withdraw_ShouldFail_WhenCurrentUserIsNotRequestExpert()
    {
        Guid expertId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();
        Startup startup = SeedStartup();
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);
        var sut = new WithdrawExpertCollaborationRequestCommandHandler(
            _db,
            new TestUserContext(otherUserId),
            _clock);

        Result result = await sut.Handle(new WithdrawExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

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
        var sut = new WithdrawExpertCollaborationRequestCommandHandler(
            _db,
            new TestUserContext(expertId),
            _clock);

        Result result = await sut.Handle(new WithdrawExpertCollaborationRequestCommand(request.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.MustBePending);
    }

    [Fact]
    public async Task GetById_ShouldReturnRequest_WhenCurrentUserIsRequestExpert()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId, "Expert Name");
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);
        var sut = new GetExpertCollaborationRequestByIdQueryHandler(
            _db,
            new TestUserContext(expertId));

        Result<ExpertCollaborationRequestResponse> result = await sut.Handle(
            new GetExpertCollaborationRequestByIdQuery(request.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(request.Id);
        result.Value.ExpertDisplayName.ShouldBe("Expert Name");
        result.Value.StartupName.ShouldBe(startup.Name);
    }

    [Fact]
    public async Task GetById_ShouldFail_WhenCurrentUserIsNotParticipant()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);
        var sut = new GetExpertCollaborationRequestByIdQueryHandler(
            _db,
            new TestUserContext(Guid.NewGuid()));

        Result<ExpertCollaborationRequestResponse> result = await sut.Handle(
            new GetExpertCollaborationRequestByIdQuery(request.Id),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.Unauthorized);
    }

    [Fact]
    public async Task GetAllByStartupId_ShouldReturnRequests_WhenCurrentUserIsFounder()
    {
        Guid expertId = Guid.NewGuid();
        Guid founderId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId);
        SeedStartupMember(founderId, startup.Id, StartupRole.Founder);
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);
        var sut = new GetExpertCollaborationRequestsByStartupIdQueryHandler(
            _db,
            new TestUserContext(founderId));

        Result<List<ExpertCollaborationRequestResponse>> result = await sut.Handle(
            new GetExpertCollaborationRequestsByStartupIdQuery(startup.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(r => r.Id).ShouldBe([request.Id]);
    }

    [Fact]
    public async Task GetAllByExpertProfileId_ShouldReturnRequests_WhenCurrentUserOwnsExpertProfile()
    {
        Guid expertId = Guid.NewGuid();
        Startup startup = SeedStartup();
        SeedExpertProfile(expertId);
        ExpertCollaborationRequest request = SeedRequest(expertId, startup.Id);
        var sut = new GetExpertCollaborationRequestsByExpertProfileIdQueryHandler(
            _db,
            new TestUserContext(expertId));

        Result<List<ExpertCollaborationRequestResponse>> result = await sut.Handle(
            new GetExpertCollaborationRequestsByExpertProfileIdQuery(expertId),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(r => r.Id).ShouldBe([request.Id]);
    }

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

    private CreateExpertCollaborationRequestCommandHandler CreateHandler(Guid userId) =>
        new(_db, new TestUserContext(userId), _clock);

    private static CreateExpertCollaborationRequestCommand CreateCommand(Guid startupId) =>
        new(
            startupId,
            CollaborationType.Advisor,
            "I can help with product strategy.",
            proposedHoursPerWeek: 8,
            proposedRate: 100m);

    private ExpertProfile SeedExpertProfile(Guid userId, string displayName = "Expert")
    {
        ExpertProfile expertProfile = ExpertProfile.Create(
            userId,
            displayName,
            bio: null,
            website: null,
            isPublic: true,
            linkedInUrl: null,
            twitterUrl: null,
            gitHubUrl: null,
            telegramUrl: null,
            Now);

        _db.ExpertProfiles.Add(expertProfile);
        _db.SaveChanges();

        return expertProfile;
    }

    private Startup SeedStartup()
    {
        Startup startup = Startup.Create(
            "Startup",
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

    private ExpertCollaborationRequest SeedRequest(Guid expertId, Guid startupId)
    {
        ExpertCollaborationRequest request = ExpertCollaborationRequest.Create(
            expertId,
            startupId,
            CollaborationType.Consultant,
            message: null,
            proposedHoursPerWeek: null,
            proposedRate: null,
            Now);

        _db.ExpertCollaborationRequests.Add(request);
        _db.SaveChanges();

        return request;
    }
}
