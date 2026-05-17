using DevStart.SharedKernel;

namespace DevStart.Domain.RefreshTokens
{
    public sealed class RefreshToken : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string TokenHash { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public Guid? ReplacedByTokenId { get; set; }
        public string? CreatedByIp { get; set; }
        public string? UserAgent { get; set; }

        public bool IsRevoked => RevokedAt is not null;
        public bool IsExpired(DateTime now) => now >= ExpiresAt;
        public bool IsActive(DateTime now) => !IsRevoked && !IsExpired(now);

        public RefreshToken()
        {
        }

        public static RefreshToken Create(
            Guid userId,
            string tokenHash,
            DateTime now,
            TimeSpan lifetime,
            string? createdByIp,
            string? userAgent)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiresAt = now.Add(lifetime),
                CreatedByIp = createdByIp,
                UserAgent = userAgent,
            };

        public void Revoke(DateTime now, Guid? replacedByTokenId = null)
        {
            RevokedAt = now;
            ReplacedByTokenId = replacedByTokenId;
        }
    }
}
