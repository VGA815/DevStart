using DevStart.SharedKernel;

namespace DevStart.Domain.EmailVerificationTokens
{
    public sealed class EmailVerificationToken : Entity
    {
        public Guid TokenId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
