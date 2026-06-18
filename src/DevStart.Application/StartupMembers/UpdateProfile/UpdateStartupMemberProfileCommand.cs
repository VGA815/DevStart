using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;

namespace DevStart.Application.StartupMembers.UpdateProfile
{
    public sealed class UpdateStartupMemberProfileCommand : ICommand
    {
        public Guid StartupId { get; set; }
        public StartupPosition? Position { get; set; }
        public int? YearsOfExperience { get; set; }
        public bool? HasPriorExit { get; set; }
        public int? PreviousStartupsCount { get; set; }

        public UpdateStartupMemberProfileCommand(Guid startupId, StartupPosition? position, int? yearsOfExperience,
            bool? hasPriorExit, int? previousStartupsCount)
        {
            StartupId = startupId;
            Position = position;
            YearsOfExperience = yearsOfExperience;
            HasPriorExit = hasPriorExit;
            PreviousStartupsCount = previousStartupsCount;
        }
    }
}
