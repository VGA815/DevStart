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
        public static StartupInvestor Create(Guid profileId, Guid startupId, bool isPublic, DateTime createdAt)
            => new()
            {
                ProfileId = profileId,
                CreatedAt = createdAt,
                IsPublic = isPublic,
                StartupId = startupId,
                UpdatedAt = createdAt
            };
        public StartupInvestor()
        {
            
        }
    }
}
