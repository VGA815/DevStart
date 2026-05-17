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
        var result = _validator.Validate(new CreateExpertProfileCommand(
            "Expert",
            "Bio",
            "https://expert.example.com",
            isPublic: true,
            linkedInUrl: "https://linkedin.com/in/expert",
            twitterUrl: null,
            gitHubUrl: null,
            telegramUrl: null,
            specializations: new List<ExpertSpecialization> { ExpertSpecialization.Engineering }));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForEmptyDisplayNameTooLongFieldsAndEmptySpecializations()
    {
        var result = _validator.Validate(new CreateExpertProfileCommand(
            string.Empty,
            new string('b', 2001),
            new string('w', 501),
            isPublic: true,
            linkedInUrl: new string('l', 501),
            twitterUrl: new string('t', 501),
            gitHubUrl: new string('g', 501),
            telegramUrl: new string('m', 501),
            specializations: new List<ExpertSpecialization>()));

        result.IsValid.ShouldBeFalse();
        var failedProperties = result.Errors.Select(error => error.PropertyName).ToList();
        failedProperties.ShouldContain("DisplayName");
        failedProperties.ShouldContain("Bio");
        failedProperties.ShouldContain("Website");
        failedProperties.ShouldContain("LinkedInUrl");
        failedProperties.ShouldContain("TwitterUrl");
        failedProperties.ShouldContain("GitHubUrl");
        failedProperties.ShouldContain("TelegramUrl");
        failedProperties.ShouldContain("Specializations");
    }

    [Fact]
    public void Validate_ShouldFail_ForInvalidSpecializationEnum()
    {
        var result = _validator.Validate(new CreateExpertProfileCommand(
            "Expert",
            null,
            null,
            isPublic: true,
            linkedInUrl: null,
            twitterUrl: null,
            gitHubUrl: null,
            telegramUrl: null,
            specializations: new List<ExpertSpecialization> { (ExpertSpecialization)999 }));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName)
            .ShouldContain(p => p.StartsWith("Specializations"));
    }
}
