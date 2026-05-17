using DevStart.Application;
using DevStart.Application.ExpertExperiences.Create;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation;

public sealed class CreateExpertExperienceCommandValidatorTests
{
    private static IValidator<CreateExpertExperienceCommand> CreateValidator(FixedDateTimeProvider? clock = null)
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<IDateTimeProvider>(clock ?? new FixedDateTimeProvider());

        return services
            .BuildServiceProvider()
            .GetRequiredService<IValidator<CreateExpertExperienceCommand>>();
    }

    [Fact]
    public void Validate_ShouldPass_ForValidExperience()
    {
        IValidator<CreateExpertExperienceCommand> validator = CreateValidator();

        var result = validator.Validate(new CreateExpertExperienceCommand(
            Guid.NewGuid(),
            "Acme",
            "Engineer",
            new DateOnly(2020, 1, 1),
            new DateOnly(2022, 12, 31),
            "Did work."));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForEmptyCompanyAndPosition()
    {
        IValidator<CreateExpertExperienceCommand> validator = CreateValidator();

        var result = validator.Validate(new CreateExpertExperienceCommand(
            Guid.NewGuid(),
            string.Empty,
            string.Empty,
            new DateOnly(2020, 1, 1),
            null,
            null));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("Company");
        result.Errors.Select(error => error.PropertyName).ShouldContain("Position");
    }

    [Fact]
    public void Validate_ShouldFail_WhenStartDateIsAfterEndDate()
    {
        IValidator<CreateExpertExperienceCommand> validator = CreateValidator();

        var result = validator.Validate(new CreateExpertExperienceCommand(
            Guid.NewGuid(),
            "Acme",
            "Engineer",
            new DateOnly(2022, 12, 31),
            new DateOnly(2020, 1, 1),
            null));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("EndDate");
    }

    [Fact]
    public void Validate_ShouldFail_WhenStartDateIsInFuture()
    {
        var clock = new FixedDateTimeProvider { UtcNow = new DateTime(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc) };
        IValidator<CreateExpertExperienceCommand> validator = CreateValidator(clock);

        var result = validator.Validate(new CreateExpertExperienceCommand(
            Guid.NewGuid(),
            "Acme",
            "Engineer",
            new DateOnly(2030, 1, 1),
            null,
            null));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("StartDate");
    }
}
