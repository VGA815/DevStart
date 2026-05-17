using DevStart.Application;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.InvestmentDeals;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Deals;

public sealed class CapTableCalculatorTests
{
    private readonly ICapTableCalculator _calculator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<ICapTableCalculator>();

    [Fact]
    public void Compute_ShouldDiluteExistingHoldersAndAppendInvestor_ForSafe()
    {
        InvestmentDeal deal = CreateDeal(InvestmentInstrument.Safe, amount: 10_000_000m, valuationCap: 50_000_000m);

        CapTableResult result = _calculator.Compute(deal, [
            new EquityHolderInput(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Founder 1", "Founder", 60m),
            new EquityHolderInput(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Founder 2", "Founder", 40m)
        ]);

        result.InvestorSharePct.ShouldBe(20m);
        result.FoundersTotalAfterPct.ShouldBe(80m);
        result.Warnings.ShouldBeEmpty();
        result.Entries.Count.ShouldBe(3);
        result.Entries[0].SharePctAfter.ShouldBe(48m);
        result.Entries[1].SharePctAfter.ShouldBe(32m);
        result.Entries[2].PartyId.ShouldBe(deal.InvestorProfileId);
        result.Entries[2].PartyName.ShouldBe("New Investor");
        result.Entries[2].PartyType.ShouldBe("Investor");
        result.Entries[2].SharePctBefore.ShouldBe(0m);
        result.Entries[2].SharePctAfter.ShouldBe(20m);
    }

    [Fact]
    public void Compute_ShouldIncludeInterestInConvertibleLoanShare()
    {
        InvestmentDeal deal = CreateDeal(
            InvestmentInstrument.ConvertibleLoan,
            amount: 10_000_000m,
            valuationCap: 50_000_000m,
            interestRate: 0.12m,
            termMonths: 24);

        CapTableResult result = _calculator.Compute(deal, [
            new EquityHolderInput(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Founder", "Founder", 100m)
        ]);

        result.InvestorSharePct.ShouldBe(24.80m);
        result.FoundersTotalAfterPct.ShouldBe(75.20m);
    }

    [Fact]
    public void Compute_ShouldUsePostMoneyFormulaForPricedRound()
    {
        InvestmentDeal deal = CreateDeal(
            InvestmentInstrument.PricedRound,
            amount: 50_000_000m,
            preMoneyValuation: 150_000_000m);

        CapTableResult result = _calculator.Compute(deal, [
            new EquityHolderInput(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Founder", "Founder", 100m)
        ]);

        result.InvestorSharePct.ShouldBe(25m);
        result.FoundersTotalAfterPct.ShouldBe(75m);
    }

    [Fact]
    public void Compute_ShouldFlagFounderFloorAndInvestorCeilingWarnings()
    {
        InvestmentDeal deal = CreateDeal(InvestmentInstrument.Safe, amount: 40_000_000m, valuationCap: 100_000_000m);

        CapTableResult result = _calculator.Compute(deal, [
            new EquityHolderInput(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Founder", "Founder", 50m),
            new EquityHolderInput(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Advisor", "Advisor", 50m)
        ]);

        result.InvestorSharePct.ShouldBe(40m);
        result.FoundersTotalAfterPct.ShouldBe(30m);
        result.Warnings.Select(warning => warning.Code).ShouldBe([
            "cap_table.founders_below_floor",
            "cap_table.investor_above_ceiling"
        ]);
    }

    private static InvestmentDeal CreateDeal(
        InvestmentInstrument instrument,
        decimal amount,
        decimal? valuationCap = null,
        decimal? interestRate = null,
        int? termMonths = null,
        decimal? preMoneyValuation = null)
    {
        InvestmentApplication application = InvestmentApplication.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            roadmapItemId: null,
            amount,
            message: null,
            new DateTime(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc),
            instrument,
            valuationCap,
            discount: null,
            interestRate,
            termMonths,
            preMoneyValuation);

        return InvestmentDeal.CreateFromApplication(
            application,
            new DateTime(2026, 5, 16, 10, 5, 0, DateTimeKind.Utc));
    }
}
