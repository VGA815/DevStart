using DevStart.Application;
using DevStart.Application.Abstractions.Validation;
using DevStart.Domain.InvestmentApplications;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Deals;

public sealed class DealTermsValidatorTests
{
    private readonly IDealTermsValidator _validator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IDealTermsValidator>();

    [Fact]
    public void Validate_ShouldReturnNoFlags_ForConservativeSafeTerms()
    {
        IReadOnlyList<DealTermsFlag> flags = _validator.Validate(new DealTermsInput(
            InvestmentInstrument.Safe,
            Amount: 5_000_000m,
            ValuationCap: 50_000_000m,
            Discount: 0.15m,
            InterestRate: null,
            TermMonths: null,
            PreMoneyValuation: null,
            LiquidationPreference: 1.0m,
            ProRataRights: false));

        flags.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_ShouldFlagAggressiveDiscountHighLiquidationPreferenceAndHighDilution()
    {
        IReadOnlyList<DealTermsFlag> flags = _validator.Validate(new DealTermsInput(
            InvestmentInstrument.Safe,
            Amount: 20_000_000m,
            ValuationCap: 50_000_000m,
            Discount: 0.30m,
            InterestRate: null,
            TermMonths: null,
            PreMoneyValuation: null,
            LiquidationPreference: 1.5m,
            ProRataRights: true));

        flags.Select(flag => flag.Code).ShouldBe([
            "deal_terms.aggressive_discount",
            "deal_terms.high_liq_pref",
            "deal_terms.high_dilution"
        ]);
        flags.ShouldAllBe(flag => flag.Severity == "warning");
    }

    [Fact]
    public void Validate_ShouldFlagHighInterestRate_ForConvertibleLoan()
    {
        IReadOnlyList<DealTermsFlag> flags = _validator.Validate(new DealTermsInput(
            InvestmentInstrument.ConvertibleLoan,
            Amount: 5_000_000m,
            ValuationCap: 50_000_000m,
            Discount: null,
            InterestRate: 0.09m,
            TermMonths: 24,
            PreMoneyValuation: null,
            LiquidationPreference: 1.0m,
            ProRataRights: false));

        flags.Select(flag => flag.Code).ShouldContain("deal_terms.high_interest_rate");
    }

    [Fact]
    public void Validate_ShouldComputeDilutionForPricedRound()
    {
        IReadOnlyList<DealTermsFlag> flags = _validator.Validate(new DealTermsInput(
            InvestmentInstrument.PricedRound,
            Amount: 50_000_000m,
            ValuationCap: null,
            Discount: null,
            InterestRate: null,
            TermMonths: null,
            PreMoneyValuation: 100_000_000m,
            LiquidationPreference: 1.0m,
            ProRataRights: false));

        flags.Select(flag => flag.Code).ShouldContain("deal_terms.high_dilution");
    }
}
