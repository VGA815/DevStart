using DevStart.SharedKernel;

namespace DevStart.Domain.StartupMembers
{
    public sealed class StartupMember : Entity
    {
        public Guid ProfileId { get; set; }
        public Guid StartupId { get; set; }
        public StartupRole Role { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public StartupMember(Guid profileId, Guid startupId, StartupRole role, bool isPublic, IDateTimeProvider dateTimeProvider)
        {
            ProfileId = profileId;
            StartupId = startupId;
            Role = role;
            IsPublic = isPublic;
            CreatedAt = dateTimeProvider.UtcNow;
            UpdatedAt = dateTimeProvider.UtcNow;
        }
    }
}
