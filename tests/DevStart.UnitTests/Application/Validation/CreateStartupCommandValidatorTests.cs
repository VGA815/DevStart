using DevStart.Application;
using DevStart.Application.Startups.Create;
using DevStart.Domain.Startups;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation;

public sealed class CreateStartupCommandValidatorTests
{
    private readonly IValidator<CreateStartupCommand> _validator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IValidator<CreateStartupCommand>>();

    [Fact]
    public void Validate_ShouldPass_ForValidStartup()
    {
        var result = _validator.Validate(CreateValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForMissingRequiredFields()
    {
        CreateStartupCommand command = CreateValidCommand();
        command.Name = string.Empty;
        command.PublicEmail = string.Empty;
        command.ProductSolution = string.Empty;

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("Name");
        result.Errors.Select(error => error.PropertyName).ShouldContain("PublicEmail");
        result.Errors.Select(error => error.PropertyName).ShouldContain("ProductSolution");
    }

    [Fact]
    public void Validate_ShouldFail_ForMalformedPublicEmail()
    {
        CreateStartupCommand command = CreateValidCommand();
        command.PublicEmail = "not-an-email";

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("PublicEmail");
    }

    // Stack, value proposition, differentiators and the problem statement are scoring inputs, not
    // gates: a startup must be creatable without them and complete them later in the editor.
    [Fact]
    public void Validate_ShouldPass_WithoutOptionalProductDetails()
    {
        CreateStartupCommand command = CreateValidCommand();
        command.Stack = [];
        command.ProductProblem = null;
        command.ProductValueProposition = null;
        command.ProductDifferentiators = null;

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForNegativeMarketFieldsAndInvalidStage()
    {
        CreateStartupCommand command = CreateValidCommand();
        command.Stage = (StartupStage)999;
        command.Tam = -1m;
        command.Sam = -1m;
        command.Som = -1m;
        command.MarketGrowthRate = -1m;

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("Stage");
        result.Errors.Select(error => error.PropertyName).ShouldContain("Tam");
        result.Errors.Select(error => error.PropertyName).ShouldContain("Sam");
        result.Errors.Select(error => error.PropertyName).ShouldContain("Som");
        result.Errors.Select(error => error.PropertyName).ShouldContain("MarketGrowthRate");
    }

    private static CreateStartupCommand CreateValidCommand() =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Startup",
            "public@example.com",
            "Description",
            "https://startup.example.com",
            isStopped: false,
            StartupStage.Mvp,
            socialMediaLinks: [],
            StartupLocation.Russia,
            "billing@example.com",
            avatarId: null,
            "Short",
            "Problem",
            "Solution",
            ["dotnet"],
            "Value",
            "Differentiators",
            tam: 1m,
            sam: 1m,
            som: 1m,
            marketGrowthRate: 1m,
            hasPatents: false);
}
