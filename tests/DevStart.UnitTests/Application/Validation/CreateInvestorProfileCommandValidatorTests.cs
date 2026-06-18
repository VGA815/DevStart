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
        var result = _validator.Validate(new CreateInvestorProfileCommand(InvestorProfileType.Individual));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForInvalidEnum()
    {
        var result = _validator.Validate(new CreateInvestorProfileCommand((InvestorProfileType)999));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("Type");
    }
}
