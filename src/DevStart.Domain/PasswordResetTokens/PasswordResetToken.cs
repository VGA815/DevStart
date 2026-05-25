using DevStart.SharedKernel;

namespace DevStart.Domain.PasswordResetTokens
{
    public sealed class PasswordResetToken : Entity
    {
        public Guid TokenId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        public PasswordResetToken()
        {

        }

        public static PasswordResetToken Create(Guid userId, DateTime createdAt, DateTime expiresAt)
            => new()
            {
                TokenId = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = createdAt,
                ExpiresAt = expiresAt
            };
    }
}
