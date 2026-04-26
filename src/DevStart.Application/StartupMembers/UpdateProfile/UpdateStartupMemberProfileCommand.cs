using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;

namespace DevStart.Application.StartupMembers.UpdateProfile
{
    public sealed class UpdateStartupMemberProfileCommand : ICommand
    {
        public Guid StartupId { get; set; }
        public StartupPosition? Position { get; set; }
        public string? Bio { get; set; }
        public int? YearsOfExperience { get; set; }

        public UpdateStartupMemberProfileCommand(Guid startupId, StartupPosition? position, string? bio, int? yearsOfExperience)
        {
            StartupId = startupId;
            Position = position;
            Bio = bio;
            YearsOfExperience = yearsOfExperience;
        }
    }
}
