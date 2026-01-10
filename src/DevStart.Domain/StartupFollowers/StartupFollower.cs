using DevStart.SharedKernel;

namespace DevStart.Domain.StartupFollowers
{
    public sealed class StartupFollower : Entity
    {
        public Guid ProfileId { get; set; }
        public Guid StartupId { get; set; }
        public DateTime CreatedAt { get; set; }

        public StartupFollower(Guid profileId, Guid startupId, IDateTimeProvider dateTimeProvider)
        {
            ProfileId = profileId;
            StartupId = startupId;
            CreatedAt = dateTimeProvider.UtcNow;
        }
    }
}
