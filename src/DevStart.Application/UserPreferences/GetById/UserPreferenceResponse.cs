using DevStart.Domain.UserPreferences;

namespace DevStart.Application.UserPreferences.GetById
{
    public class UserPreferenceResponse
    {
        public Guid UserId { get; set; }
        public UserPreferenceTheme Theme { get; set; }
        public bool ReceiveNotifications { get; set; }
    }
}