using DevStart.Application;
using DevStart.Application.InvestorProfiles.Create;
using DevStart.Domain.Investors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation;

public sealed class CreateInvestorProfileCommandValidatorTests
{
    private readonly IValidator<CreateInvestorProfileCommand> _validator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IValidator<CreateInvestorProfileCommand>>();

    [Fact]
    public void Validate_ShouldPass_ForValidInvestorProfile()
    {
        var result = _validator.Validate(Command());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForInvalidEnum()
    {
        var result = _validator.Validate(Command(type: (InvestorProfileType)999));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("Type");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_ForMissingDisplayName(string displayName)
    {
        var result = _validator.Validate(Command(displayName: displayName));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("DisplayName");
    }

    [Fact]
    public void Validate_ShouldFail_ForTooLongDisplayName()
    {
        var result = _validator.Validate(Command(displayName: new string('x', 201)));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("DisplayName");
    }

    [Fact]
    public void Validate_ShouldFail_ForTooLongBio()
    {
        var result = _validator.Validate(Command(bio: new string('x', 2001)));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("Bio");
    }

    [Fact]
    public void Validate_ShouldFail_ForTooLongWebsite()
    {
        var result = _validator.Validate(Command(website: new string('x', 501)));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("Website");
    }

    private static CreateInvestorProfileCommand Command(
        InvestorProfileType type = InvestorProfileType.Individual,
        string displayName = "Jane Investor",
        string? bio = null,
        string? website = null) =>
        new(type, displayName, bio, website, isPublic: true);
}
