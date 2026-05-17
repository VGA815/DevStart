using DevStart.Application;
using DevStart.Application.InvestmentApplications.Accept;
using DevStart.Application.InvestmentApplications.Reject;
using DevStart.Application.InvestmentApplications.Withdraw;
using DevStart.Application.InvestmentDeals.ConfirmByInvestor;
using DevStart.Application.InvestmentDeals.ConfirmByStartup;
using DevStart.Application.Notifications.MarkAsRead;
using DevStart.Application.StartupFollowers.Create;
using DevStart.Application.StartupFollowers.Delete;
using DevStart.Application.StartupInvestors.ChangeVisibility;
using DevStart.Application.StartupInvestors.Create;
using DevStart.Application.StartupMembers.ChangeRole;
using DevStart.Application.StartupMembers.ChangeVisibility;
using DevStart.Application.StartupMembers.Create;
using DevStart.Application.StartupMembers.Delete;
using DevStart.Application.StartupMembers.UpdateProfile;
using DevStart.Application.UserPreferences.Create;
using DevStart.Application.UserPreferences.Delete;
using DevStart.Application.UserPreferences.Update;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.UserPreferences;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Validation;

public sealed class SimpleCommandValidatorTests
{
    private static readonly Guid Id1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Id2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly ServiceProvider _serviceProvider = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider();

    [Fact]
    public void InvestmentApplicationActionValidators_ShouldRequireApplicationId()
    {
        ShouldValidate(new AcceptInvestmentApplicationCommand(Id1), new AcceptInvestmentApplicationCommand(Guid.Empty), "ApplicationId");
        ShouldValidate(new RejectInvestmentApplicationCommand(Id1), new RejectInvestmentApplicationCommand(Guid.Empty), "ApplicationId");
        ShouldValidate(new WithdrawInvestmentApplicationCommand(Id1), new WithdrawInvestmentApplicationCommand(Guid.Empty), "ApplicationId");
    }

    [Fact]
    public void InvestmentDealConfirmationValidators_ShouldRequireDealId()
    {
        ShouldValidate(new ConfirmInvestmentDealByStartupCommand(Id1), new ConfirmInvestmentDealByStartupCommand(Guid.Empty), "DealId");
        ShouldValidate(new ConfirmInvestmentDealByInvestorCommand(Id1), new ConfirmInvestmentDealByInvestorCommand(Guid.Empty), "DealId");
    }

    [Fact]
    public void NotificationValidator_ShouldRequireNotificationId()
    {
        ShouldValidate(new MarkNotificationAsReadCommand(Id1), new MarkNotificationAsReadCommand(Guid.Empty), "NotificationId");
    }

    [Fact]
    public void UserPreferenceValidators_ShouldRequireUserIdAndValidTheme()
    {
        ShouldValidate(
            new CreateUserPreferenceCommand { UserId = Id1, Theme = UserPreferenceTheme.Dark, ReceiveNotifications = true },
            new CreateUserPreferenceCommand { UserId = Guid.Empty, Theme = (UserPreferenceTheme)999, ReceiveNotifications = true },
            "UserId",
            "Theme");
        ShouldValidate(
            new UpdateUserPreferenceCommand(Id1, UserPreferenceTheme.Light, receiveNotifications: false),
            new UpdateUserPreferenceCommand(Guid.Empty, (UserPreferenceTheme)999, receiveNotifications: false),
            "UserId",
            "Theme");
        ShouldValidate(new DeleteUserPreferenceCommand(Id1), new DeleteUserPreferenceCommand(Guid.Empty), "UserId");
    }

    [Fact]
    public void StartupFollowerValidators_ShouldRequireIds()
    {
        ShouldValidate(new CreateStartupFollowerCommand(Id1, Id2), new CreateStartupFollowerCommand(Guid.Empty, Guid.Empty), "StartupId", "ProfileId");
        ShouldValidate(new DeleteStartupFollowerCommand(Id1), new DeleteStartupFollowerCommand(Guid.Empty), "StartupId");
    }

    [Fact]
    public void StartupInvestorValidators_ShouldRequireIds()
    {
        ShouldValidate(new CreateStartupInvestorCommand(Id1, Id2, isPublic: true), new CreateStartupInvestorCommand(Guid.Empty, Guid.Empty, isPublic: true), "StartupId", "ProfileId");
        ShouldValidate(new ChangeStartupInvestorVisibilityCommand(Id1, isPublic: true), new ChangeStartupInvestorVisibilityCommand(Guid.Empty, isPublic: true), "StartupId");
    }

    [Fact]
    public void StartupMemberValidators_ShouldRequireIdsAndValidEnums()
    {
        ShouldValidate(
            new CreateStartupMemberCommand(Id1, Id2, StartupRole.Founder, isPublic: true, StartupPosition.CEO, "Bio", 1, false, 0),
            new CreateStartupMemberCommand(Guid.Empty, Guid.Empty, (StartupRole)999, isPublic: true, (StartupPosition)999, new string('b', 2001), -1, false, -1),
            "ProfileId",
            "StartupId",
            "Role",
            "Position",
            "Bio",
            "YearsOfExperience",
            "PreviousStartupsCount");

        ShouldValidate(
            new ChangeStartupMemberRoleCommand(Id1, Id2, StartupRole.Member),
            new ChangeStartupMemberRoleCommand(Guid.Empty, Guid.Empty, (StartupRole)999),
            "StartupId",
            "ProfileId",
            "Role");

        ShouldValidate(
            new ChangeStartupMemberVisibilityCommand(Id1, Id2, isPublic: true),
            new ChangeStartupMemberVisibilityCommand(Guid.Empty, Guid.Empty, isPublic: true),
            "StartupId",
            "ProfileId");

        ShouldValidate(
            new UpdateStartupMemberProfileCommand(Id1, StartupPosition.CTO, "Bio", 1, false, 0),
            new UpdateStartupMemberProfileCommand(Guid.Empty, (StartupPosition)999, new string('b', 2001), -1, false, -1),
            "StartupId",
            "Position",
            "Bio",
            "YearsOfExperience",
            "PreviousStartupsCount");

        ShouldValidate(new DeleteStartupMemberCommand(Id1, Id2), new DeleteStartupMemberCommand(Guid.Empty, Guid.Empty), "StartupId", "ProfileId");
    }

    private void ShouldValidate<TCommand>(TCommand validCommand, TCommand invalidCommand, params string[] expectedInvalidProperties)
    {
        IValidator<TCommand> validator = _serviceProvider.GetRequiredService<IValidator<TCommand>>();

        validator.Validate(validCommand).IsValid.ShouldBeTrue();

        var invalidResult = validator.Validate(invalidCommand);

        invalidResult.IsValid.ShouldBeFalse();
        string[] invalidProperties = invalidResult.Errors.Select(error => error.PropertyName).ToArray();
        foreach (string expectedInvalidProperty in expectedInvalidProperties)
        {
            invalidProperties.ShouldContain(expectedInvalidProperty);
        }
    }
}
