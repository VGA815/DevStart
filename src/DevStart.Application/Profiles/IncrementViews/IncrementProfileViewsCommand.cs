using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Profiles.IncrementViews
{
    public sealed record IncrementProfileViewsCommand(Guid ProfileUserId) : ICommand;
}
