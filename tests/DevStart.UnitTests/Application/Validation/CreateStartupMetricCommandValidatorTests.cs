using DevStart.Application;
using DevStart.Application.StartupMetrics.Create;
using DevStart.Domain.StartupMetrics;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation;

public sealed class CreateStartupMetricCommandValidatorTests
{
    private readonly IValidator<CreateStartupMetricCommand> _validator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IValidator<CreateStartupMetricCommand>>();

    [Fact]
    public void Validate_ShouldPass_ForValidMetric()
    {
        var result = _validator.Validate(new CreateStartupMetricCommand(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            MetricType.Mrr,
            100_000m));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForEmptyStartupAndInvalidMetricType()
    {
        var result = _validator.Validate(new CreateStartupMetricCommand(
            Guid.Empty,
            (MetricType)999,
            100_000m));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("StartupId");
        result.Errors.Select(error => error.PropertyName).ShouldContain("MetricType");
    }
}
