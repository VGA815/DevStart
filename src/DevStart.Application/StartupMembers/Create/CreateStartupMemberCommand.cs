using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;

namespace DevStart.Application.StartupMembers.Create
{
    public sealed class CreateStartupMemberCommand : ICommand<Guid>
    {
        public Guid ProfileId { get; set; }
        public Guid StartupId { get; set; }
        public StartupRole Role { get; set; }
        public bool IsPublic { get; set; }

        public CreateStartupMemberCommand(Guid profileId, Guid startupId, StartupRole role, bool isPublic)
        {
            ProfileId = profileId;
            StartupId = startupId;
            Role = role;
            IsPublic = isPublic;
        }
    }
}
