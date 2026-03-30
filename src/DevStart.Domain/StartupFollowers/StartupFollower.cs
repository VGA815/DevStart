using DevStart.SharedKernel;

namespace DevStart.Domain.StartupFollowers
{
    public sealed class StartupFollower : Entity
    {
        public Guid ProfileId { get; set; }
        public Guid StartupId { get; set; }
        public DateTime CreatedAt { get; set; }

        public StartupFollower()
        {
            
        }
        public static StartupFollower Create(Guid ProfileId, Guid StartupId, DateTime CreatedAt)
            => new () { StartupId = StartupId, ProfileId = ProfileId, CreatedAt = CreatedAt };
    }
}
