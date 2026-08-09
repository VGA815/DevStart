using DevStart.Application.ExpertCollaborationRequests;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Infrastructure.Database;
using DevStart.Infrastructure.ExpertCollaborationRequests;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.ExpertCollaborationRequests;

public sealed class ExpertCollaborationRequestExpiryJobTests
{
    private static readonly DateTime Now = new(2026, 5, 28, 10, 0, 0, DateTimeKind.Utc);
    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FixedDateTimeProvider _clock = new() { UtcNow = Now };
    private readonly ExpertCollaborationOptions _options = new();

    [Fact]
    public async Task Run_ShouldExpirePendingRequestsOlderThanTheTtl()
    {
        ExpertCollaborationRequest stale = SeedPending(Now.AddDays(-31));

        await Job().RunAsync(CancellationToken.None);

        ExpertCollaborationRequest reloaded = await _db.ExpertCollaborationRequests.SingleAsync(r => r.Id == stale.Id);
        reloaded.Status.ShouldBe(ExpertCollaborationRequestStatus.Expired);
        reloaded.UpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Run_ShouldLeaveYoungPendingRequestsAlone()
    {
        ExpertCollaborationRequest fresh = SeedPending(Now.AddDays(-29));

        await Job().RunAsync(CancellationToken.None);

        ExpertCollaborationRequest reloaded = await _db.ExpertCollaborationRequests.SingleAsync(r => r.Id == fresh.Id);
        reloaded.Status.ShouldBe(ExpertCollaborationRequestStatus.Pending);
    }

    [Fact]
    public async Task Run_ShouldNotTouchAlreadyAnsweredRequests()
    {
        ExpertCollaborationRequest answered = SeedPending(Now.AddDays(-90));
        answered.Accept(Now.AddDays(-89));
        await _db.SaveChangesAsync();

        await Job().RunAsync(CancellationToken.None);

        ExpertCollaborationRequest reloaded = await _db.ExpertCollaborationRequests.SingleAsync(r => r.Id == answered.Id);
        reloaded.Status.ShouldBe(ExpertCollaborationRequestStatus.Accepted);
        reloaded.UpdatedAt.ShouldBe(Now.AddDays(-89));
    }

    [Fact]
    public async Task Run_ShouldDoNothing_WhenExpiryIsDisabled()
    {
        ExpertCollaborationRequest stale = SeedPending(Now.AddDays(-365));
        _options.PendingTtlDays = 0;

        await Job().RunAsync(CancellationToken.None);

        ExpertCollaborationRequest reloaded = await _db.ExpertCollaborationRequests.SingleAsync(r => r.Id == stale.Id);
        reloaded.Status.ShouldBe(ExpertCollaborationRequestStatus.Pending);
    }

    private ExpertCollaborationRequestExpiryJob Job() =>
        new(_db,
            _clock,
            Options.Create(_options),
            NullLogger<ExpertCollaborationRequestExpiryJob>.Instance);

    private ExpertCollaborationRequest SeedPending(DateTime createdAt)
    {
        ExpertCollaborationRequest request = ExpertCollaborationRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CollaborationRequestInitiator.Expert,
            CollaborationType.Advisor,
            message: null,
            proposedHoursPerWeek: null,
            proposedRate: null,
            createdAt);

        _db.ExpertCollaborationRequests.Add(request);
        _db.SaveChanges();

        return request;
    }
}
