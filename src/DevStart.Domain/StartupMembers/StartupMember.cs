using DevStart.Domain.Profiles;
using DevStart.SharedKernel;

namespace DevStart.Domain.StartupMembers
{
    public sealed class StartupMember : Entity
    {
        public Guid ProfileId { get; set; }
        public Guid StartupId { get; set; }
        public StartupRole Role { get; set; }
        public bool IsPublic { get; set; }
        public StartupPosition? Position { get; set; }
        public int? YearsOfExperience { get; set; }
        public bool? HasPriorExit { get; set; }
        public int? PreviousStartupsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Personal data (name, bio, links) lives on the shared Profile, referenced by ProfileId (= UserId).
        // StartupMember only carries membership-scoped data (role, position, experience, in-team visibility).
        public Profile Profile { get; set; } = null!;

        public StartupMember()
        {

        }
        public static StartupMember Create(
            Guid profileId, Guid startupId, StartupRole role, bool isPublic, DateTime createdAt,
            StartupPosition? position = null, int? yearsOfExperience = null,
            bool? hasPriorExit = null, int? previousStartupsCount = null)
            => new()
            {
                ProfileId = profileId,
                CreatedAt = createdAt,
                IsPublic = isPublic,
                Role = role,
                StartupId = startupId,
                UpdatedAt = createdAt,
                Position = position,
                YearsOfExperience = yearsOfExperience,
                HasPriorExit = hasPriorExit,
                PreviousStartupsCount = previousStartupsCount
            };

        public Result UpdateProfile(
            StartupPosition? position,
            int? yearsOfExperience,
            bool? hasPriorExit,
            int? previousStartupsCount,
            DateTime utcNow)
        {
            Position = position;
            YearsOfExperience = yearsOfExperience;
            HasPriorExit = hasPriorExit;
            PreviousStartupsCount = previousStartupsCount;
            UpdatedAt = utcNow;
            return Result.Success();
        }
    }
}
