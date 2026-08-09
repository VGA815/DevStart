using DevStart.Application;
using DevStart.Application.ExpertCollaborationRequests.Create;
using DevStart.Domain.ExpertCollaborationRequests;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation;

public sealed class CreateExpertCollaborationRequestCommandValidatorTests
{
    private readonly IValidator<CreateExpertCollaborationRequestCommand> _validator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IValidator<CreateExpertCollaborationRequestCommand>>();

    [Fact]
    public void Validate_ShouldPass_ForValidRequest()
    {
        var result = _validator.Validate(new CreateExpertCollaborationRequestCommand(
            startupId: Guid.NewGuid(),
            expertProfileId: null,
            collaborationType: CollaborationType.Advisor,
            message: "Hello",
            proposedHoursPerWeek: 10,
            proposedRate: 50m));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForEmptyStartupIdAndInvalidEnum()
    {
        var result = _validator.Validate(new CreateExpertCollaborationRequestCommand(
            startupId: Guid.Empty,
            expertProfileId: null,
            collaborationType: (CollaborationType)999,
            message: null,
            proposedHoursPerWeek: null,
            proposedRate: null));

        result.IsValid.ShouldBeFalse();
        var properties = result.Errors.Select(e => e.PropertyName).ToList();
        properties.ShouldContain("StartupId");
        properties.ShouldContain("CollaborationType");
    }

    [Fact]
    public void Validate_ShouldFail_ForTooLongMessage()
    {
        var result = _validator.Validate(new CreateExpertCollaborationRequestCommand(
            startupId: Guid.NewGuid(),
            expertProfileId: null,
            collaborationType: CollaborationType.Mentor,
            message: new string('x', 2001),
            proposedHoursPerWeek: null,
            proposedRate: null));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(e => e.PropertyName).ShouldContain("Message");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(169)]
    public void Validate_ShouldFail_ForOutOfRangeProposedHours(int hours)
    {
        var result = _validator.Validate(new CreateExpertCollaborationRequestCommand(
            startupId: Guid.NewGuid(),
            expertProfileId: null,
            collaborationType: CollaborationType.Consultant,
            message: null,
            proposedHoursPerWeek: hours,
            proposedRate: null));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(e => e.PropertyName).ShouldContain("ProposedHoursPerWeek");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Validate_ShouldFail_ForNonPositiveProposedRate(decimal rate)
    {
        var result = _validator.Validate(new CreateExpertCollaborationRequestCommand(
            startupId: Guid.NewGuid(),
            expertProfileId: null,
            collaborationType: CollaborationType.ProjectBased,
            message: null,
            proposedHoursPerWeek: null,
            proposedRate: rate));

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(e => e.PropertyName).ShouldContain("ProposedRate");
    }
}
