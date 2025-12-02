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
    }
}
