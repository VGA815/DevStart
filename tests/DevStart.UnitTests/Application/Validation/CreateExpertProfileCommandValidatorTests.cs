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
            new List<ExpertSpecialization> { ExpertSpecialization.Engineering }));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForEmptySpecializations()
    {
        var result = _validator.Validate(new CreateExpertProfileCommand(
            new List<ExpertSpecialization>()));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("Specializations");
    }

    [Fact]
    public void Validate_ShouldFail_ForInvalidSpecializationEnum()
    {
        var result = _validator.Validate(new CreateExpertProfileCommand(
            new List<ExpertSpecialization> { (ExpertSpecialization)999 }));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName)
            .ShouldContain(p => p.StartsWith("Specializations"));
    }
}
