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

        /// <summary>
        /// Root of the rotation chain: equal to <see cref="Id"/> on the first token of a login and
        /// copied onto every replacement. This is what the sessions list exposes and what the access
        /// token's <c>sid</c> claim carries — the row id would go stale on the first refresh.
        /// </summary>
        public Guid SessionId { get; set; }

        /// <summary>When the session was first established; copied across rotations, unlike <see cref="CreatedAt"/>.</summary>
        public DateTime SessionStartedAt { get; set; }

        public DateTime LastUsedAt { get; set; }

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
        {
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiresAt = now.Add(lifetime),
                CreatedByIp = createdByIp,
                UserAgent = userAgent,
                SessionStartedAt = now,
                LastUsedAt = now,
            };

            token.SessionId = token.Id;
            return token;
        }

        /// <summary>
        /// The next token in an existing session's rotation chain: a new secret, but the same session
        /// identity, so the sessions list and the <c>sid</c> claim survive the refresh.
        /// </summary>
        public static RefreshToken CreateReplacement(
            RefreshToken previous,
            string tokenHash,
            DateTime now,
            TimeSpan lifetime,
            string? createdByIp,
            string? userAgent)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = previous.UserId,
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiresAt = now.Add(lifetime),
                CreatedByIp = createdByIp,
                UserAgent = userAgent,
                SessionId = previous.SessionId,
                SessionStartedAt = previous.SessionStartedAt,
                LastUsedAt = now,
            };

        public void Revoke(DateTime now, Guid? replacedByTokenId = null)
        {
            RevokedAt = now;
            ReplacedByTokenId = replacedByTokenId;
        }
    }
}
