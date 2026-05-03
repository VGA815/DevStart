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
        public StartupPosition? Position { get; set; }
        public string? Bio { get; set; }
        public int? YearsOfExperience { get; set; }
        public bool? HasPriorExit { get; set; }
        public int? PreviousStartupsCount { get; set; }

        public CreateStartupMemberCommand(Guid profileId, Guid startupId, StartupRole role, bool isPublic,
            StartupPosition? position = null, string? bio = null, int? yearsOfExperience = null,
            bool? hasPriorExit = null, int? previousStartupsCount = null)
        {
            ProfileId = profileId;
            StartupId = startupId;
            Role = role;
            IsPublic = isPublic;
            Position = position;
            Bio = bio;
            YearsOfExperience = yearsOfExperience;
            HasPriorExit = hasPriorExit;
            PreviousStartupsCount = previousStartupsCount;
        }
    }
}
