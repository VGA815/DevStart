using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Validation;
using DevStart.Application.InvestmentApplications.SuggestedTerms;
using DevStart.Application.Scoring;
using DevStart.Application.Startups.GetScore;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.Subscriptions;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.InvestmentApplications.SuggestedTerms;

public sealed class GetSuggestedTermsQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 7, 4, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly Guid _startupId = Guid.NewGuid();

    private GetSuggestedTermsQueryHandler CreateSut(ScoreResult score, bool hasPro = true) =>
        new(new StubComputeHandler(Result.Success(score)),
            new TestUserContext(Guid.NewGuid()),
            new StubSubscriptionChecker(hasPro),
            _db,
            new DealTermsValidator());

    private static ScoreResult Score(decimal low, decimal high, decimal point, params string[] methods) =>
        new(TotalScore: 70m, TeamScore: 70m, MarketScore: 70m, ProductScore: 70m,
            TractionScore: 70m, CompetitionScore: 70m,
            ValuationLow: low, ValuationHigh: high,
            MethodsUsed: methods, CalculatedAt: Now, ValuationPoint: point);

    [Fact]
    public async Task NonMemberWithoutPro_IsRejected()
    {
        GetSuggestedTermsQueryHandler sut = CreateSut(Score(60_000_000m, 100_000_000m, 80_000_000m, "Berkus"), hasPro: false);

        Result<SuggestedTermsResponse> result = await sut.Handle(
            new GetSuggestedTermsQuery(_startupId, InvestmentInstrument.Safe, 1_000_000m), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SubscriptionErrors.ProRequired);
    }

    [Fact]
    public async Task InsufficientData_WhenNoMethodContributed()
    {
        GetSuggestedTermsQueryHandler sut = CreateSut(Score(0m, 0m, 0m));

        Result<SuggestedTermsResponse> result = await sut.Handle(
            new GetSuggestedTermsQuery(_startupId, InvestmentInstrument.Safe, 1_000_000m), CancellationToken.None);

        // Never suggest a ₽0 cap as if it were a real valuation.
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ValuationErrors.InsufficientData);
    }

    [Fact]
    public async Task InsufficientData_WhenValuationHighIsZero_EvenWithMethods()
    {
        // Real case: pre-revenue SeriesA whose target round amount wipes the VC pre-money to 0.
        GetSuggestedTermsQueryHandler sut = CreateSut(Score(0m, 0m, 0m, "VcMethod"));

        Result<SuggestedTermsResponse> result = await sut.Handle(
            new GetSuggestedTermsQuery(_startupId, InvestmentInstrument.PricedRound, 1_000_000m), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ValuationErrors.InsufficientData);
    }

    [Fact]
    public async Task Safe_SuggestsCapFromHigh_WithImpliedShareAndNoWarnings_ForModerateAmount()
    {
        GetSuggestedTermsQueryHandler sut = CreateSut(Score(60_000_000m, 100_000_000m, 80_000_000m, "Berkus", "Scorecard"));

        Result<SuggestedTermsResponse> result = await sut.Handle(
            new GetSuggestedTermsQuery(_startupId, InvestmentInstrument.Safe, 21_000_000m), CancellationToken.None);

        SuggestedTermsResponse r = result.Value;
        r.SuggestedValuationCap.ShouldBe(105_000_000m); // high × 1.05
        r.SuggestedDiscount.ShouldBe(0.20m);
        r.SuggestedInterestRate.ShouldBeNull();
        r.SuggestedPreMoneyValuation.ShouldBeNull();
        r.ImpliedInvestorSharePct.ShouldBe(20m);        // 21M / 105M
        r.Warnings.ShouldBeEmpty();
        r.ValuationLowReference.ShouldBe(60_000_000m);
        r.ValuationHighReference.ShouldBe(100_000_000m);
    }

    [Fact]
    public async Task Safe_FlagsHighDilution_WhenAmountImpliesOver30Percent()
    {
        GetSuggestedTermsQueryHandler sut = CreateSut(Score(60_000_000m, 100_000_000m, 80_000_000m, "Berkus"));

        Result<SuggestedTermsResponse> result = await sut.Handle(
            new GetSuggestedTermsQuery(_startupId, InvestmentInstrument.Safe, 42_000_000m), CancellationToken.None);

        SuggestedTermsResponse r = result.Value;
        r.ImpliedInvestorSharePct.ShouldBe(40m); // 42M / 105M
        r.Warnings.Select(w => w.Code).ShouldContain("deal_terms.high_dilution");
    }

    [Fact]
    public async Task ConvertibleLoan_ImpliedShareIncludesAccruedInterest()
    {
        GetSuggestedTermsQueryHandler sut = CreateSut(Score(60_000_000m, 100_000_000m, 80_000_000m, "Berkus"));

        Result<SuggestedTermsResponse> result = await sut.Handle(
            new GetSuggestedTermsQuery(_startupId, InvestmentInstrument.ConvertibleLoan, 21_000_000m), CancellationToken.None);

        SuggestedTermsResponse r = result.Value;
        r.SuggestedValuationCap.ShouldBe(105_000_000m);
        r.SuggestedInterestRate.ShouldBe(0.06m);
        r.SuggestedTermMonths.ShouldBe(18);
        // 21M × (1 + 0.06 × 18/12) = 22.89M → 22.89M / 105M = 21.8%
        r.ImpliedInvestorSharePct.ShouldBe(21.8m);
    }

    [Fact]
    public async Task PricedRound_SuggestsEnsemblePointAsPreMoney()
    {
        GetSuggestedTermsQueryHandler sut = CreateSut(Score(60_000_000m, 100_000_000m, 80_000_000m, "Berkus"));

        Result<SuggestedTermsResponse> result = await sut.Handle(
            new GetSuggestedTermsQuery(_startupId, InvestmentInstrument.PricedRound, 20_000_000m), CancellationToken.None);

        SuggestedTermsResponse r = result.Value;
        // The point estimate, not the SAFE-cap's high × 1.05 anchor.
        r.SuggestedPreMoneyValuation.ShouldBe(80_000_000m);
        r.SuggestedValuationCap.ShouldBeNull();
        r.ImpliedInvestorSharePct.ShouldBe(20m); // 20M / (80M + 20M)
    }

    [Fact]
    public async Task SuggestedCap_RoundsMidpointsAwayFromZero()
    {
        // high 10 → ×1.05 = 10.5 → 11 away-from-zero (banker's rounding would yield 10).
        GetSuggestedTermsQueryHandler sut = CreateSut(Score(5m, 10m, 8m, "Berkus"));

        Result<SuggestedTermsResponse> result = await sut.Handle(
            new GetSuggestedTermsQuery(_startupId, InvestmentInstrument.Safe, 1m), CancellationToken.None);

        result.Value.SuggestedValuationCap.ShouldBe(11m);
    }

    private sealed class StubComputeHandler(Result<ScoreResult> result)
        : IQueryHandler<ComputeStartupScoreQuery, ScoreResult>
    {
        public Task<Result<ScoreResult>> Handle(ComputeStartupScoreQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }
}
