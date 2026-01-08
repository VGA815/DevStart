using DevStart.Domain.StartupMembers;

namespace DevStart.Application.StartupMembers.GetAllByStartupId
{
    public sealed class StartupMemberResponse
    {
        public Guid ProfileId { get; set; }
        public Guid StartupId { get; set; }
        public StartupRole Role { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}