using DevStart.Application;
using DevStart.Application.Messages.Create;
using DevStart.Domain.Messages;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation;

public sealed class CreateMessageCommandValidatorTests
{
    private readonly IValidator<CreateMessageCommand> _validator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IValidator<CreateMessageCommand>>();

    [Fact]
    public void Validate_ShouldPass_ForValidMessage()
    {
        var result = _validator.Validate(new CreateMessageCommand
        {
            ReceiverId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ReceiverType = ChatParticipantType.User,
            TextContent = "Hello"
        });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_ForEmptyReceiverAndInvalidReceiverType()
    {
        var result = _validator.Validate(new CreateMessageCommand
        {
            ReceiverId = Guid.Empty,
            ReceiverType = (ChatParticipantType)999
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("ReceiverId");
        result.Errors.Select(error => error.PropertyName).ShouldContain("ReceiverType");
    }

    [Fact]
    public void Validate_ShouldFail_ForTextOverTheLengthLimit()
    {
        var result = _validator.Validate(new CreateMessageCommand
        {
            ReceiverId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ReceiverType = ChatParticipantType.User,
            TextContent = new string('a', MessageRules.MaxTextLength + 1)
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("TextContent");
    }

    [Fact]
    public void Validate_ShouldFail_ForTooManyAttachmentsOfOneKind()
    {
        var result = _validator.Validate(new CreateMessageCommand
        {
            ReceiverId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ReceiverType = ChatParticipantType.User,
            DocumentIds = [.. Enumerable.Range(0, MessageRules.MaxAttachmentsPerKind + 1).Select(_ => Guid.NewGuid())]
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("DocumentIds");
    }

    [Fact]
    public void Validate_ShouldPass_ForAttachmentsWithinTheLimit()
    {
        var result = _validator.Validate(new CreateMessageCommand
        {
            ReceiverId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ReceiverType = ChatParticipantType.User,
            DocumentIds = [Guid.NewGuid()],
            FileIds = [Guid.NewGuid()]
        });

        result.IsValid.ShouldBeTrue();
    }
}
