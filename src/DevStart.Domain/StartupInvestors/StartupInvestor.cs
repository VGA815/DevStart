using DevStart.SharedKernel;

namespace DevStart.Domain.StartupInvestors
{
    public sealed class StartupInvestor : Entity
    {
        public Guid ProfileId { get; set; }
        public Guid StartupId { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public StartupInvestor(Guid profileId, Guid startupId, bool isPublic, DateTime createdAt, DateTime updatedAt)
        {
            ProfileId = profileId;
            StartupId = startupId;
            IsPublic = isPublic;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }
    }
}
