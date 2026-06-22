using DevStart.Application.Startups.GetAll;
using DevStart.Application.Startups.GetById;
using DevStart.Domain.Startups;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;
using ListResponse = DevStart.Application.Startups.GetAll.StartupResponse;
using DetailResponse = DevStart.Application.Startups.GetById.StartupResponse;

namespace DevStart.UnitTests.Application.Startups;

public sealed class StartupBanVisibilityTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FixedDateTimeProvider _clock = new() { UtcNow = Now };

    private Startup Seed(Action<Startup>? mutate = null)
    {
        Startup s = Startup.Create("Acme", "acme@example.com", null, null, default, null, null, null, Now, null, null);
        mutate?.Invoke(s);
        _db.Startups.Add(s);
        return s;
    }

    private async Task<List<ListResponse>> ListAsync()
    {
        var handler = new GetStartupsQueryHandler(_db, _clock);
        Result<List<ListResponse>> result = await handler.Handle(new GetStartupsQuery(1, 50), default);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private Task<Result<DetailResponse>> GetByIdAsync(Guid id) =>
        new GetStartupByIdQueryHandler(_db, _clock).Handle(new GetStartupByIdQuery(id), default);

    [Fact]
    public async Task PermanentlyBanned_HiddenFromListAndDetail()
    {
        Startup visible = Seed();
        Startup banned = Seed(s => s.Ban("spam", null, Guid.NewGuid(), Now));
        await _db.SaveChangesAsync();

        (await ListAsync()).Select(x => x.Id).ShouldBe(new[] { visible.Id });
        (await GetByIdAsync(banned.Id)).IsFailure.ShouldBeTrue();
        (await GetByIdAsync(visible.Id)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ActiveTemporaryBan_Hidden()
    {
        Startup banned = Seed(s => s.Ban("temp", Now.AddDays(1), Guid.NewGuid(), Now));
        await _db.SaveChangesAsync();

        (await ListAsync()).ShouldBeEmpty();
        (await GetByIdAsync(banned.Id)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task ExpiredTemporaryBan_VisibleWithoutWaitingForJob()
    {
        // Banned in the past with an expiry already elapsed at query time (Now); the hourly job hasn't run.
        Startup banned = Seed(s => s.Ban("temp", Now.AddDays(-1), Guid.NewGuid(), Now.AddDays(-3)));
        await _db.SaveChangesAsync();

        banned.IsBanned.ShouldBeTrue(); // flag still set, but lazy expiry applies

        (await ListAsync()).Select(x => x.Id).ShouldContain(banned.Id);
        (await GetByIdAsync(banned.Id)).IsSuccess.ShouldBeTrue();
    }
}
