using DevStart.Application;
using DevStart.Application.ExpertProfiles.Create;
using DevStart.Domain.Experts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation;

public sealed class CreateExpertProfileCommandValidatorTests
{
    private readonly IValidator<CreateExpertProfileCommand> _validator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IValidator<CreateExpertProfileCommand>>();

    [Fact]
    public void Validate_ShouldPass_ForValidExpertProfile()
    {
        var result = _validator.Validate(Command());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForEmptySpecializations()
    {
        var result = _validator.Validate(Command(specializations: []));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("Specializations");
    }

    [Fact]
    public void Validate_ShouldFail_ForInvalidSpecializationEnum()
    {
        var result = _validator.Validate(Command(specializations: [(ExpertSpecialization)999]));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName)
            .ShouldContain(p => p.StartsWith("Specializations"));
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

    [Theory]
    [InlineData("Website")]
    [InlineData("LinkedInUrl")]
    [InlineData("TwitterUrl")]
    [InlineData("GitHubUrl")]
    [InlineData("TelegramUrl")]
    public void Validate_ShouldFail_ForTooLongLinks(string property)
    {
        string tooLong = new('x', 501);

        var result = _validator.Validate(Command(
            website: property == "Website" ? tooLong : null,
            linkedInUrl: property == "LinkedInUrl" ? tooLong : null,
            twitterUrl: property == "TwitterUrl" ? tooLong : null,
            gitHubUrl: property == "GitHubUrl" ? tooLong : null,
            telegramUrl: property == "TelegramUrl" ? tooLong : null));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain(property);
    }

    private static CreateExpertProfileCommand Command(
        List<ExpertSpecialization>? specializations = null,
        string displayName = "Jane Expert",
        string? bio = null,
        string? website = null,
        string? linkedInUrl = null,
        string? twitterUrl = null,
        string? gitHubUrl = null,
        string? telegramUrl = null) =>
        new(specializations ?? [ExpertSpecialization.Engineering],
            displayName,
            bio,
            website,
            isPublic: true,
            linkedInUrl,
            twitterUrl,
            gitHubUrl,
            telegramUrl);
}
