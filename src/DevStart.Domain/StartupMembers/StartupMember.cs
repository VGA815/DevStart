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
        public StartupMember()
        {
            
        }
        public static StartupMember Create(Guid profileId, Guid startupId, StartupRole role, bool isPublic, DateTime createdAt)
            => new()
            {
                ProfileId = profileId,
                CreatedAt = createdAt,
                IsPublic = isPublic,
                Role = role,
                StartupId = startupId,
                UpdatedAt = createdAt
            };
    }
}
