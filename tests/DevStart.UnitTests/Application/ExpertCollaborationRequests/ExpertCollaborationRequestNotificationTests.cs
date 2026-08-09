using DevStart.Application.ExpertCollaborationRequests.Accept;
using DevStart.Application.ExpertCollaborationRequests.Create;
using DevStart.Application.ExpertCollaborationRequests.Expire;
using DevStart.Application.ExpertCollaborationRequests.Reject;
using DevStart.Application.ExpertCollaborationRequests.Withdraw;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Notifications;
using DevStart.Domain.StartupMembers;
using DevStart.Infrastructure.Database;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.ExpertCollaborationRequests;

/// <summary>
/// Each lifecycle event must reach the opposite side of whoever acted, and carry a type the client
/// can route from. Both are direction-dependent, so every case is checked in both directions.
/// </summary>
public sealed class ExpertCollaborationRequestNotificationTests
{
    private static readonly DateTime Now = new(2026, 5, 28, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ExpertId = Guid.NewGuid();
    private static readonly Guid StartupId = Guid.NewGuid();
    private static readonly Guid FounderId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid RequestId = Guid.NewGuid();

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FixedDateTimeProvider _clock = new() { UtcNow = Now };
    private readonly RecordingNotificationService _notifications = new();

    public ExpertCollaborationRequestNotificationTests()
    {
        // A plain member is deliberately included: they can neither answer nor withdraw, so they are
        // not a recipient either.
        _db.StartupMembers.AddRange(
            StartupMember.Create(FounderId, StartupId, StartupRole.Founder, isPublic: true, Now),
            StartupMember.Create(AdminId, StartupId, StartupRole.Administration, isPublic: true, Now),
            StartupMember.Create(Guid.NewGuid(), StartupId, StartupRole.Member, isPublic: true, Now));
        _db.SaveChanges();
    }

    [Fact]
    public async Task Created_ByExpert_ShouldNotifyTheStartupLeadership()
    {
        await new ExpertCollaborationRequestCreatedDomainEventHandler(_db, _notifications, _clock).Handle(
            new ExpertCollaborationRequestCreatedDomainEvent(
                RequestId, ExpertId, StartupId, CollaborationRequestInitiator.Expert, CollaborationType.Advisor),
            CancellationToken.None);

        Recipients().ShouldBe([FounderId, AdminId], ignoreOrder: true);
        Types().ShouldAllBe(t => t == NotificationType.ExpertCollaborationRequestReceived);
    }

    [Fact]
    public async Task Created_ByStartup_ShouldNotifyOnlyTheInvitedExpert()
    {
        await new ExpertCollaborationRequestCreatedDomainEventHandler(_db, _notifications, _clock).Handle(
            new ExpertCollaborationRequestCreatedDomainEvent(
                RequestId, ExpertId, StartupId, CollaborationRequestInitiator.Startup, CollaborationType.Advisor),
            CancellationToken.None);

        Recipients().ShouldBe([ExpertId]);
        Types().ShouldBe([NotificationType.ExpertCollaborationInvitationReceived]);
    }

    [Fact]
    public async Task Accepted_ShouldNotifyTheInitiatorSide()
    {
        await new ExpertCollaborationRequestAcceptedDomainEventHandler(_db, _notifications, _clock).Handle(
            new ExpertCollaborationRequestAcceptedDomainEvent(
                RequestId, ExpertId, StartupId, CollaborationRequestInitiator.Expert),
            CancellationToken.None);

        Recipients().ShouldBe([ExpertId]);
        Types().ShouldBe([NotificationType.ExpertCollaborationRequestAccepted]);
    }

    [Fact]
    public async Task Accepted_ForAnInvitation_ShouldNotifyTheStartupLeadership()
    {
        await new ExpertCollaborationRequestAcceptedDomainEventHandler(_db, _notifications, _clock).Handle(
            new ExpertCollaborationRequestAcceptedDomainEvent(
                RequestId, ExpertId, StartupId, CollaborationRequestInitiator.Startup),
            CancellationToken.None);

        Recipients().ShouldBe([FounderId, AdminId], ignoreOrder: true);
        Types().ShouldAllBe(t => t == NotificationType.ExpertCollaborationInvitationAccepted);
    }

    [Fact]
    public async Task Rejected_ForAnInvitation_ShouldNotifyTheStartupLeadership()
    {
        await new ExpertCollaborationRequestRejectedDomainEventHandler(_db, _notifications, _clock).Handle(
            new ExpertCollaborationRequestRejectedDomainEvent(
                RequestId, ExpertId, StartupId, CollaborationRequestInitiator.Startup),
            CancellationToken.None);

        Recipients().ShouldBe([FounderId, AdminId], ignoreOrder: true);
        Types().ShouldAllBe(t => t == NotificationType.ExpertCollaborationInvitationRejected);
    }

    [Fact]
    public async Task Withdrawn_ByExpert_ShouldNotifyTheStartupLeadership()
    {
        await new ExpertCollaborationRequestWithdrawnDomainEventHandler(_db, _notifications, _clock).Handle(
            new ExpertCollaborationRequestWithdrawnDomainEvent(
                RequestId, ExpertId, StartupId, CollaborationRequestInitiator.Expert),
            CancellationToken.None);

        Recipients().ShouldBe([FounderId, AdminId], ignoreOrder: true);
        Types().ShouldAllBe(t => t == NotificationType.ExpertCollaborationRequestWithdrawn);
    }

    [Fact]
    public async Task Withdrawn_ByStartup_ShouldNotifyTheInvitedExpert()
    {
        await new ExpertCollaborationRequestWithdrawnDomainEventHandler(_db, _notifications, _clock).Handle(
            new ExpertCollaborationRequestWithdrawnDomainEvent(
                RequestId, ExpertId, StartupId, CollaborationRequestInitiator.Startup),
            CancellationToken.None);

        Recipients().ShouldBe([ExpertId]);
        Types().ShouldBe([NotificationType.ExpertCollaborationInvitationWithdrawn]);
    }

    [Fact]
    public async Task Expired_ShouldNotifyOnlyTheSideThatWasWaiting()
    {
        await new ExpertCollaborationRequestExpiredDomainEventHandler(_db, _notifications, _clock).Handle(
            new ExpertCollaborationRequestExpiredDomainEvent(
                RequestId, ExpertId, StartupId, CollaborationRequestInitiator.Expert),
            CancellationToken.None);

        Recipients().ShouldBe([ExpertId]);
        Types().ShouldBe([NotificationType.ExpertCollaborationRequestExpired]);
    }

    [Fact]
    public async Task Expired_ForAnInvitation_ShouldNotifyTheStartupLeadership()
    {
        await new ExpertCollaborationRequestExpiredDomainEventHandler(_db, _notifications, _clock).Handle(
            new ExpertCollaborationRequestExpiredDomainEvent(
                RequestId, ExpertId, StartupId, CollaborationRequestInitiator.Startup),
            CancellationToken.None);

        Recipients().ShouldBe([FounderId, AdminId], ignoreOrder: true);
        Types().ShouldAllBe(t => t == NotificationType.ExpertCollaborationInvitationExpired);
    }

    [Fact]
    public async Task EveryNotification_ShouldReferenceTheRequest()
    {
        await new ExpertCollaborationRequestCreatedDomainEventHandler(_db, _notifications, _clock).Handle(
            new ExpertCollaborationRequestCreatedDomainEvent(
                RequestId, ExpertId, StartupId, CollaborationRequestInitiator.Startup, CollaborationType.Mentor),
            CancellationToken.None);

        _notifications.Published.ShouldAllBe(n => n.ReferenceId == RequestId);
        _notifications.Published.ShouldAllBe(n => n.CreatedAt == Now);
    }

    private List<Guid> Recipients() => [.. _notifications.Published.Select(n => n.UserId)];

    private List<NotificationType> Types() => [.. _notifications.Published.Select(n => n.Type)];
}
