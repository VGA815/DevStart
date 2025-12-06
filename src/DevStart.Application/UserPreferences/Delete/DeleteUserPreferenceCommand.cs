using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.UserPreferences.Delete
{
    public sealed record DeleteUserPreferenceCommand(Guid UserId) : ICommand;
}
