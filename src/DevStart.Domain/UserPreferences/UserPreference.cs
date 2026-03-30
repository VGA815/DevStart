using DevStart.SharedKernel;

namespace DevStart.Domain.UserPreferences
{
    public sealed class UserPreference : Entity
    {
        public Guid UserId { get; set; }
        public UserPreferenceTheme Theme { get; set; }
        public bool ReceiveNotifications { get; set; }
        public UserPreference() {}
        public static UserPreference Create(Guid userId, UserPreferenceTheme theme) 
            => new () { UserId = userId, Theme = theme, ReceiveNotifications = true };
    }
}
