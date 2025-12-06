using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Profiles.Delete
{
    public sealed record DeleteProfileCommand(Guid UserId) : ICommand;
}
