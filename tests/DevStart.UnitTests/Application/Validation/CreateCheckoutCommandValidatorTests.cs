using DevStart.Application;
using DevStart.Application.Subscriptions.Checkout;
using DevStart.Domain.Subscriptions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation;

public sealed class CreateCheckoutCommandValidatorTests
{
    private readonly IValidator<CreateCheckoutCommand> _validator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IValidator<CreateCheckoutCommand>>();

    [Fact]
    public void Validate_ShouldPass_ForProPlan()
    {
        var result = _validator.Validate(new CreateCheckoutCommand(SubscriptionPlan.Pro));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForNonProPlan()
    {
        var result = _validator.Validate(new CreateCheckoutCommand(SubscriptionPlan.Free));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("Plan");
    }
}
