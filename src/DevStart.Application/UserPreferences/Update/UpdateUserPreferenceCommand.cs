using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.UserPreferences;

namespace DevStart.Application.UserPreferences.Update
{
    public sealed class UpdateUserPreferenceCommand : ICommand
    {
        public Guid UserId { get; set; }
        public UserPreferenceTheme Theme { get; set; }
        public bool ReceiveNotifications { get; set; }
    }
}
