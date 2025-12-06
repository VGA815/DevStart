using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.UserPreferences;

namespace DevStart.Application.UserPreferences.Create
{
    public sealed class CreateUserPreferenceCommand : ICommand<Guid>
    {
        public Guid UserId { get; set; }
        public bool ReceiveNotifications { get; set; }
        public UserPreferenceTheme Theme { get; set; }
    }
}
