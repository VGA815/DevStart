using DevStart.Domain.ExpertCollaborationRequests;
using Shouldly;

namespace DevStart.UnitTests.Domain.ExpertCollaborationRequests;

public sealed class ExpertCollaborationRequestTests
{
    private static readonly Guid ExpertId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid StartupId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTime CreatedAt = new(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UpdatedAt = new(2026, 5, 17, 11, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldInitializeRequestWithPendingStatus()
    {
        ExpertCollaborationRequest request = ExpertCollaborationRequest.Create(
            ExpertId,
            StartupId,
            CollaborationRequestInitiator.Expert,
            CollaborationType.Advisor,
            "I'd like to help.",
            proposedHoursPerWeek: 5,
            proposedRate: 100m,
            CreatedAt);

        request.Id.ShouldNotBe(Guid.Empty);
        request.ExpertProfileId.ShouldBe(ExpertId);
        request.StartupId.ShouldBe(StartupId);
        request.Initiator.ShouldBe(CollaborationRequestInitiator.Expert);
        request.AwaitsExpertResponse.ShouldBeFalse();
        request.CollaborationType.ShouldBe(CollaborationType.Advisor);
        request.Message.ShouldBe("I'd like to help.");
        request.ProposedHoursPerWeek.ShouldBe(5);
        request.ProposedRate.ShouldBe(100m);
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Pending);
        request.CreatedAt.ShouldBe(CreatedAt);
        request.UpdatedAt.ShouldBe(CreatedAt);
    }

    [Fact]
    public void Accept_FromPending_ShouldTransitionToAccepted()
    {
        ExpertCollaborationRequest request = CreatePending();

        var result = request.Accept(UpdatedAt);

        result.IsSuccess.ShouldBeTrue();
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Accepted);
        request.UpdatedAt.ShouldBe(UpdatedAt);
    }

    [Fact]
    public void Reject_FromPending_ShouldTransitionToRejected()
    {
        ExpertCollaborationRequest request = CreatePending();

        var result = request.Reject(UpdatedAt);

        result.IsSuccess.ShouldBeTrue();
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Rejected);
        request.UpdatedAt.ShouldBe(UpdatedAt);
    }

    [Fact]
    public void Withdraw_FromPending_ShouldTransitionToWithdrawn()
    {
        ExpertCollaborationRequest request = CreatePending();

        var result = request.Withdraw(UpdatedAt);

        result.IsSuccess.ShouldBeTrue();
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Withdrawn);
        request.UpdatedAt.ShouldBe(UpdatedAt);
    }

    [Fact]
    public void Accept_FromNonPending_ShouldFail()
    {
        ExpertCollaborationRequest request = CreatePending();
        request.Accept(UpdatedAt);

        var second = request.Accept(UpdatedAt);

        second.IsFailure.ShouldBeTrue();
        second.Error.ShouldBe(ExpertCollaborationRequestErrors.MustBePending);
    }

    [Fact]
    public void Reject_FromNonPending_ShouldFail()
    {
        ExpertCollaborationRequest request = CreatePending();
        request.Withdraw(UpdatedAt);

        var result = request.Reject(UpdatedAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.MustBePending);
    }

    [Fact]
    public void Withdraw_FromNonPending_ShouldFail()
    {
        ExpertCollaborationRequest request = CreatePending();
        request.Reject(UpdatedAt);

        var result = request.Withdraw(UpdatedAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.MustBePending);
    }

    [Fact]
    public void Create_ByStartup_ShouldAwaitExpertResponse()
    {
        ExpertCollaborationRequest request = CreatePending(CollaborationRequestInitiator.Startup);

        request.Initiator.ShouldBe(CollaborationRequestInitiator.Startup);
        request.AwaitsExpertResponse.ShouldBeTrue();
    }

    [Fact]
    public void Expire_FromPending_ShouldTransitionToExpired()
    {
        ExpertCollaborationRequest request = CreatePending();

        var result = request.Expire(UpdatedAt);

        result.IsSuccess.ShouldBeTrue();
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Expired);
        request.UpdatedAt.ShouldBe(UpdatedAt);
    }

    [Fact]
    public void Expire_FromNonPending_ShouldFail()
    {
        ExpertCollaborationRequest request = CreatePending();
        request.Accept(UpdatedAt);

        var result = request.Expire(UpdatedAt);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ExpertCollaborationRequestErrors.MustBePending);
        request.Status.ShouldBe(ExpertCollaborationRequestStatus.Accepted);
    }

    [Fact]
    public void Respond_FromExpired_ShouldFail()
    {
        ExpertCollaborationRequest request = CreatePending();
        request.Expire(UpdatedAt);

        request.Accept(UpdatedAt).Error.ShouldBe(ExpertCollaborationRequestErrors.MustBePending);
        request.Reject(UpdatedAt).Error.ShouldBe(ExpertCollaborationRequestErrors.MustBePending);
        request.Withdraw(UpdatedAt).Error.ShouldBe(ExpertCollaborationRequestErrors.MustBePending);
    }

    private static ExpertCollaborationRequest CreatePending(
        CollaborationRequestInitiator initiator = CollaborationRequestInitiator.Expert) =>
        ExpertCollaborationRequest.Create(
            ExpertId,
            StartupId,
            initiator,
            CollaborationType.Consultant,
            message: null,
            proposedHoursPerWeek: null,
            proposedRate: null,
            CreatedAt);
}
