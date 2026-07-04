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

    [Fact]
    public void Compute_ShouldNormalizeAfterColumnToExactly100_ForUnevenSplits()
    {
        // 10M / 100M SAFE → 10% investor, 90% dilution. Seven holders whose diluted shares each
        // round independently leave a 0.01 rounding residual that must be absorbed.
        InvestmentDeal deal = CreateDeal(InvestmentInstrument.Safe, amount: 10_000_000m, valuationCap: 100_000_000m);

        var holders = new List<EquityHolderInput>
        {
            new(Guid.NewGuid(), "Founder 1", "Founder", 14.29m),
            new(Guid.NewGuid(), "Founder 2", "Founder", 14.29m),
            new(Guid.NewGuid(), "Founder 3", "Founder", 14.29m),
            new(Guid.NewGuid(), "Founder 4", "Founder", 14.29m),
            new(Guid.NewGuid(), "Founder 5", "Founder", 14.29m),
            new(Guid.NewGuid(), "Founder 6", "Founder", 14.29m),
            new(Guid.NewGuid(), "Founder 7", "Founder", 14.26m)
        };

        CapTableResult result = _calculator.Compute(deal, holders);

        result.Entries.Sum(entry => entry.SharePctAfter).ShouldBe(100m);
        result.InvestorSharePct.ShouldBe(10m);
        result.FoundersTotalAfterPct.ShouldBe(90m);
    }

    [Fact]
    public void Compute_ShouldFlagShareCapped_WhenAmountExceedsCap()
    {
        InvestmentDeal deal = CreateDeal(InvestmentInstrument.Safe, amount: 60_000_000m, valuationCap: 50_000_000m);

        CapTableResult result = _calculator.Compute(deal, [
            new EquityHolderInput(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Founder", "Founder", 100m)
        ]);

        result.InvestorSharePct.ShouldBe(100m);
        result.FoundersTotalAfterPct.ShouldBe(0m);
        result.Entries.Sum(entry => entry.SharePctAfter).ShouldBe(100m);
        result.Warnings.Select(warning => warning.Code).ShouldContain("cap_table.share_capped");
    }

    [Fact]
    public void Compute_ShouldFlagShareCapped_WhenAmountEqualsCap()
    {
        // amount == cap → the investor takes exactly 100%; "meets or exceeds" must warn here too.
        InvestmentDeal deal = CreateDeal(InvestmentInstrument.Safe, amount: 50_000_000m, valuationCap: 50_000_000m);

        CapTableResult result = _calculator.Compute(deal, [
            new EquityHolderInput(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Founder", "Founder", 100m)
        ]);

        result.InvestorSharePct.ShouldBe(100m);
        result.FoundersTotalAfterPct.ShouldBe(0m);
        result.Warnings.Select(warning => warning.Code).ShouldContain("cap_table.share_capped");
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
