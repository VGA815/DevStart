using DevStart.Application;
using DevStart.Application.UserConsents;
using DevStart.Application.Users.Register;
using DevStart.Domain.UserConsents;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly IValidator<RegisterUserCommand> _validator = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IValidator<RegisterUserCommand>>();

    [Fact]
    public void Validate_ShouldPass_WhenRequiredFieldsAndAllConsentsAreProvided()
    {
        var result = _validator.Validate(CreateValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenRequiredFieldsAreEmpty()
    {
        RegisterUserCommand command = CreateValidCommand();
        command.Email = string.Empty;
        command.Password = string.Empty;
        command.Username = string.Empty;

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain("Email");
        result.Errors.Select(error => error.PropertyName).ShouldContain("Password");
        result.Errors.Select(error => error.PropertyName).ShouldContain("Username");
    }

    [Fact]
    public void Validate_ShouldFail_WhenConsentListIsEmpty()
    {
        RegisterUserCommand command = CreateValidCommand();
        command.Consents = [];

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == "Consents list must not be empty");
    }

    [Fact]
    public void Validate_ShouldFail_WhenConsentTypeIsMissing()
    {
        RegisterUserCommand command = CreateValidCommand();
        command.Consents =
        [
            new ConsentItem(ConsentType.PersonalDataProcessing, ConsentVersions.GetCurrentVersion(ConsentType.PersonalDataProcessing), true),
            new ConsentItem(ConsentType.PrivacyPolicy, ConsentVersions.GetCurrentVersion(ConsentType.PrivacyPolicy), true),
            new ConsentItem(ConsentType.TermsOfService, ConsentVersions.GetCurrentVersion(ConsentType.TermsOfService), true)
        ];

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage.StartsWith("All consent types must be provided:"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenConsentTypesContainDuplicates()
    {
        RegisterUserCommand command = CreateValidCommand();
        command.Consents =
        [
            new ConsentItem(ConsentType.PersonalDataProcessing, "1.0", true),
            new ConsentItem(ConsentType.PersonalDataProcessing, "1.0", true),
            new ConsentItem(ConsentType.PrivacyPolicy, "1.0", true),
            new ConsentItem(ConsentType.TermsOfService, "1.0", true),
            new ConsentItem(ConsentType.Cookies, "1.0", true)
        ];

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == "Consent list must not contain duplicate types");
    }

    [Fact]
    public void Validate_ShouldFail_WhenMandatoryConsentIsNotAccepted()
    {
        RegisterUserCommand command = CreateValidCommand();
        command.Consents =
        [
            new ConsentItem(ConsentType.PersonalDataProcessing, "1.0", true),
            new ConsentItem(ConsentType.PrivacyPolicy, "1.0", false),
            new ConsentItem(ConsentType.TermsOfService, "1.0", true),
            new ConsentItem(ConsentType.Cookies, "1.0", false)
        ];

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == "Consent 'PrivacyPolicy' is mandatory and must be accepted");
        result.Errors.ShouldNotContain(error => error.ErrorMessage == "Consent 'Cookies' is mandatory and must be accepted");
    }

    private static RegisterUserCommand CreateValidCommand() =>
        new(
            "alice@example.com",
            "password",
            "alice",
            bio: null,
            name: "Alice",
            url: null,
            socialMediaLinks: [],
            isPublic: true,
            consents:
            [
                new ConsentItem(ConsentType.PersonalDataProcessing, ConsentVersions.GetCurrentVersion(ConsentType.PersonalDataProcessing), true),
                new ConsentItem(ConsentType.PrivacyPolicy, ConsentVersions.GetCurrentVersion(ConsentType.PrivacyPolicy), true),
                new ConsentItem(ConsentType.TermsOfService, ConsentVersions.GetCurrentVersion(ConsentType.TermsOfService), true),
                new ConsentItem(ConsentType.Cookies, ConsentVersions.GetCurrentVersion(ConsentType.Cookies), false)
            ]);
}
