using DevStart.SharedKernel;

namespace DevStart.Domain.TrustedDevices
{
    /// <summary>
    /// A browser that has already completed a second factor for this user and may therefore skip the
    /// TOTP challenge until <see cref="ExpiresAt"/>. The raw token lives only in the client's
    /// localStorage; the server keeps a SHA-256 hash, exactly like <see cref="RefreshTokens.RefreshToken"/>.
    ///
    /// The token is deliberately <em>not</em> rotated on use. Rotation would have to hand a new secret
    /// back through the login response on a path that has no room for one, and a dropped response
    /// would kill the trust for no reason the user can see. Unlike a refresh token this is not a
    /// session credential — on its own it grants nothing, since the password is still required.
    /// </summary>
    public sealed class TrustedDevice : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        /// <summary>SHA-256 hex of the raw token; the raw value is never stored.</summary>
        public string TokenHash { get; set; } = null!;

        /// <summary>Human-readable name shown in the devices list, e.g. "Chrome на Windows".</summary>
        public string? Label { get; set; }
        public string? UserAgent { get; set; }

        /// <summary>The IP the device was trusted from — the subnet anchor for the strict policy.</summary>
        public string? CreatedByIp { get; set; }
        public string? LastSeenIp { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Absolute and never extended on use. A sliding window would turn the user's "30 days" into
        /// "forever" for anyone who keeps the token warm.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        public DateTime LastUsedAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public bool IsRevoked => RevokedAt is not null;
        public bool IsActive(DateTime now) => !IsRevoked && now < ExpiresAt;

        public TrustedDevice()
        {
        }

        public static TrustedDevice Create(
            Guid userId,
            string tokenHash,
            DateTime now,
            TimeSpan lifetime,
            string? createdByIp,
            string? userAgent,
            string? label)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                Label = label,
                UserAgent = userAgent,
                CreatedByIp = createdByIp,
                LastSeenIp = createdByIp,
                CreatedAt = now,
                ExpiresAt = now.Add(lifetime),
                LastUsedAt = now,
            };

        public void Touch(DateTime now, string? ip)
        {
            LastUsedAt = now;
            if (!string.IsNullOrWhiteSpace(ip))
            {
                LastSeenIp = ip;
            }
        }

        public void Revoke(DateTime now)
        {
            RevokedAt ??= now;
        }
    }
}
