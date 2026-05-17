using DevStart.Domain.UserPreferences;
using Shouldly;

namespace DevStart.UnitTests.Domain.UserPreferences;

public sealed class UserPreferenceTests
{
    [Fact]
    public void Create_ShouldInitializePreferenceWithNotificationsEnabled()
    {
        Guid userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        UserPreference preference = UserPreference.Create(userId, UserPreferenceTheme.Dark);

        preference.UserId.ShouldBe(userId);
        preference.Theme.ShouldBe(UserPreferenceTheme.Dark);
        preference.ReceiveNotifications.ShouldBeTrue();
    }
}
