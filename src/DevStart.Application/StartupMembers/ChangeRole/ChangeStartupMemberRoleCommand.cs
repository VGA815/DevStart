using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;

namespace DevStart.Application.StartupMembers.ChangeRole
{
    public sealed class ChangeStartupMemberRoleCommand : ICommand
    {
        public Guid StartupId { get; set; }
        public Guid ProfileId { get; set; }
        public StartupRole Role { get; set; }

        public ChangeStartupMemberRoleCommand(Guid startupId, Guid profileId, StartupRole role)
        {
            StartupId = startupId;
            ProfileId = profileId;
            Role = role;
        }
    }
}
