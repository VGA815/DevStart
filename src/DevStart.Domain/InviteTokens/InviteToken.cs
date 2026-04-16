using DevStart.SharedKernel;

namespace DevStart.Domain.InviteTokens
{
    public sealed class InviteToken : Entity
    {
        public Guid Id { get; set; }
        public Guid StartupId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public static InviteToken Create(Guid startupId, DateTime expiresAt)
        {
            return new InviteToken
            {
                Id = Guid.NewGuid(),
                StartupId = startupId,
                ExpiresAt = expiresAt,
                IsUsed = false
            };
        }
        public InviteToken()
        {
            
        }
    }
}
