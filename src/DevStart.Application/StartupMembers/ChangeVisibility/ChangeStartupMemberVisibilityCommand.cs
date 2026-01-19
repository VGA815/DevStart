using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupMembers.ChangeVisibility
{
    public sealed class ChangeStartupMemberVisibilityCommand : ICommand
    {
        public Guid ProfileId { get; set; }
        public Guid StartupId { get; set; }
        public bool IsPublic { get; set; }

        public ChangeStartupMemberVisibilityCommand(Guid profileId, Guid startupId, bool isPublic)
        {
            ProfileId = profileId;
            StartupId = startupId;
            IsPublic = isPublic;
        }
    }
}
