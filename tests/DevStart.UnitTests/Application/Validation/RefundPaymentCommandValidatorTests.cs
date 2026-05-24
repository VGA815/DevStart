using DevStart.Application;
using DevStart.Application.Payments.Refund;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation;

public sealed class RefundPaymentCommandValidatorTests
{
    private readonly IValidator<RefundPaymentCommand> _validator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IValidator<RefundPaymentCommand>>();

    [Fact]
    public void Validate_ShouldPass_ForFullRefund_NullAmount()
    {
        var result = _validator.Validate(new RefundPaymentCommand(Guid.NewGuid(), null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_ForPositivePartialAmount()
    {
        var result = _validator.Validate(new RefundPaymentCommand(Guid.NewGuid(), 100m));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_ShouldFail_ForNonPositiveAmount(decimal amount)
    {
        var result = _validator.Validate(new RefundPaymentCommand(Guid.NewGuid(), amount));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_ShouldFail_ForEmptyPaymentId()
    {
        var result = _validator.Validate(new RefundPaymentCommand(Guid.Empty, null));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(e => e.PropertyName).ShouldContain(nameof(RefundPaymentCommand.PaymentId));
    }
}
