using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Scoring;
using DevStart.Application.Startups.GetScore;
using DevStart.Application.Startups.RecomputeValuation;
using DevStart.Domain.Startups;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.UnitTests.Application.Startups.RecomputeValuation;

public sealed class RecomputeStartupValuationCommandHandlerTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Handle_PersistsSnapshot_WhenValuationIsAvailable()
    {
        Guid startupId = Guid.NewGuid();
        var score = new ScoreResult(
            TotalScore: 80m, TeamScore: 70m, MarketScore: 60m, ProductScore: 65m,
            TractionScore: 55m, CompetitionScore: 50m,
            ValuationLow: 100_000_000m, ValuationHigh: 200_000_000m,
            MethodsUsed: ["Scorecard", "VcMethod"], CalculatedAt: Now,
            ValuationPoint: 150_000_000m,
            ValuationMethods:
            [
                new ValuationBreakdown("Scorecard", 140_000_000m, 0.5m, ["median ₽400M"]),
                new ValuationBreakdown("VcMethod", 160_000_000m, 0.5m, ["IRR 50%"])
            ],
            MethodologyVersion: "v-test");

        var sut = new RecomputeStartupValuationCommandHandler(new StubScoreHandler(score), _db);

        Result<Guid> result = await sut.Handle(new RecomputeStartupValuationCommand(startupId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        StartupValuationSnapshot snapshot = await _db.StartupValuationSnapshots.SingleAsync();
        snapshot.Id.ShouldBe(result.Value);
        snapshot.StartupId.ShouldBe(startupId);
        snapshot.ValuationLow.ShouldBe(100_000_000m);
        snapshot.ValuationHigh.ShouldBe(200_000_000m);
        snapshot.ValuationPoint.ShouldBe(150_000_000m);
        snapshot.MethodsUsed.ShouldBe("Scorecard,VcMethod");
        snapshot.MethodologyVersion.ShouldBe("v-test");
        snapshot.BreakdownJson.ShouldNotBeNullOrEmpty();
        snapshot.BreakdownJson!.ShouldContain("VcMethod");
    }

    [Fact]
    public async Task Handle_Fails_WithoutWritingSnapshot_WhenNoMethodApplies()
    {
        var score = new ScoreResult(0, 0, 0, 0, 0, 0, 0, 0, [], Now); // insufficient data — empty methods

        var sut = new RecomputeStartupValuationCommandHandler(new StubScoreHandler(score), _db);

        Result<Guid> result = await sut.Handle(new RecomputeStartupValuationCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Valuation.InsufficientData");
        (await _db.StartupValuationSnapshots.AnyAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_PropagatesFailure_WhenScoreComputationFails()
    {
        var error = Error.NotFound("Startups.NotFound", "nope");
        var sut = new RecomputeStartupValuationCommandHandler(new StubScoreHandler(error), _db);

        Result<Guid> result = await sut.Handle(new RecomputeStartupValuationCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
        (await _db.StartupValuationSnapshots.AnyAsync()).ShouldBeFalse();
    }

    private sealed class StubScoreHandler : IQueryHandler<ComputeStartupScoreQuery, ScoreResult>
    {
        private readonly Result<ScoreResult> _result;

        public StubScoreHandler(ScoreResult score) => _result = Result.Success(score);
        public StubScoreHandler(Error error) => _result = Result.Failure<ScoreResult>(error);

        public Task<Result<ScoreResult>> Handle(ComputeStartupScoreQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(_result);
    }
}
