using DevStart.SharedKernel;

namespace DevStart.Domain.TwoFactor
{
    /// <summary>
    /// A single-use 2FA recovery code. Only the SHA-256 hash is stored; the plaintext is shown to
    /// the user exactly once at generation time.
    /// </summary>
    public sealed class TwoFactorRecoveryCode : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string CodeHash { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UsedAt { get; set; }

        public bool IsUsed => UsedAt is not null;

        public TwoFactorRecoveryCode()
        {
        }

        public static TwoFactorRecoveryCode Create(Guid userId, string codeHash, DateTime now)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CodeHash = codeHash,
                CreatedAt = now,
            };

        public void MarkUsed(DateTime now)
        {
            UsedAt = now;
        }
    }
}
