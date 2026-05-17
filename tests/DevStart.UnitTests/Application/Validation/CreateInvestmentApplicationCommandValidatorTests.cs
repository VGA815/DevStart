using DevStart.Application;
using DevStart.Application.InvestmentApplications.Create;
using DevStart.Domain.InvestmentApplications;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation;

public sealed class CreateInvestmentApplicationCommandValidatorTests
{
    private readonly IValidator<CreateInvestmentApplicationCommand> _validator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IValidator<CreateInvestmentApplicationCommand>>();

    [Theory]
    [InlineData(InvestmentInstrument.Safe)]
    [InlineData(InvestmentInstrument.ConvertibleLoan)]
    [InlineData(InvestmentInstrument.PricedRound)]
    public void Validate_ShouldPass_ForValidTermsByInstrument(InvestmentInstrument instrument)
    {
        var result = _validator.Validate(CreateValidCommand(instrument));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForCommonInvalidFields()
    {
        CreateInvestmentApplicationCommand command = CreateValidCommand(InvestmentInstrument.Safe);
        command.StartupId = Guid.Empty;
        command.Amount = 0m;
        command.Message = new string('m', 2001);
        command.Instrument = (InvestmentInstrument)999;
        command.LiquidationPreference = 0.9m;

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("StartupId");
        result.Errors.Select(error => error.PropertyName).ShouldContain("Amount");
        result.Errors.Select(error => error.PropertyName).ShouldContain("Message");
        result.Errors.Select(error => error.PropertyName).ShouldContain("Instrument");
        result.Errors.Select(error => error.PropertyName).ShouldContain("LiquidationPreference");
    }

    [Fact]
    public void Validate_ShouldRequireValuationCap_ForSafeAndConvertibleLoan()
    {
        CreateInvestmentApplicationCommand safe = CreateValidCommand(InvestmentInstrument.Safe);
        CreateInvestmentApplicationCommand convertible = CreateValidCommand(InvestmentInstrument.ConvertibleLoan);
        safe.ValuationCap = null;
        convertible.ValuationCap = null;

        var safeResult = _validator.Validate(safe);
        var convertibleResult = _validator.Validate(convertible);

        safeResult.IsValid.ShouldBeFalse();
        safeResult.Errors.Select(error => error.PropertyName).ShouldContain("ValuationCap");
        convertibleResult.IsValid.ShouldBeFalse();
        convertibleResult.Errors.Select(error => error.PropertyName).ShouldContain("ValuationCap");
    }

    [Fact]
    public void Validate_ShouldRequireConvertibleLoanSpecificTerms()
    {
        CreateInvestmentApplicationCommand command = CreateValidCommand(InvestmentInstrument.ConvertibleLoan);
        command.InterestRate = null;
        command.TermMonths = null;

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("InterestRate");
        result.Errors.Select(error => error.PropertyName).ShouldContain("TermMonths");
    }

    [Fact]
    public void Validate_ShouldRequirePreMoneyValuation_ForPricedRound()
    {
        CreateInvestmentApplicationCommand command = CreateValidCommand(InvestmentInstrument.PricedRound);
        command.PreMoneyValuation = null;

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("PreMoneyValuation");
    }

    [Fact]
    public void Validate_ShouldFail_ForOutOfRangeDealTerms()
    {
        CreateInvestmentApplicationCommand command = CreateValidCommand(InvestmentInstrument.ConvertibleLoan);
        command.Discount = 0.6m;
        command.InterestRate = 0.31m;
        command.TermMonths = 61;
        command.LiquidationPreference = 3.1m;

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("Discount");
        result.Errors.Select(error => error.PropertyName).ShouldContain("InterestRate");
        result.Errors.Select(error => error.PropertyName).ShouldContain("TermMonths");
        result.Errors.Select(error => error.PropertyName).ShouldContain("LiquidationPreference");
    }

    private static CreateInvestmentApplicationCommand CreateValidCommand(InvestmentInstrument instrument) =>
        instrument switch
        {
            InvestmentInstrument.Safe => new CreateInvestmentApplicationCommand(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                roadmapItemId: null,
                amount: 1_000_000m,
                message: "Message",
                InvestmentInstrument.Safe,
                valuationCap: 10_000_000m,
                discount: 0.2m),
            InvestmentInstrument.ConvertibleLoan => new CreateInvestmentApplicationCommand(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                roadmapItemId: null,
                amount: 1_000_000m,
                message: "Message",
                InvestmentInstrument.ConvertibleLoan,
                valuationCap: 10_000_000m,
                discount: 0.2m,
                interestRate: 0.08m,
                termMonths: 24),
            InvestmentInstrument.PricedRound => new CreateInvestmentApplicationCommand(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                roadmapItemId: null,
                amount: 1_000_000m,
                message: "Message",
                InvestmentInstrument.PricedRound,
                preMoneyValuation: 20_000_000m),
            _ => throw new ArgumentOutOfRangeException(nameof(instrument), instrument, null)
        };
}
