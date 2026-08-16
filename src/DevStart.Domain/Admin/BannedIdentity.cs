using DevStart.SharedKernel;
using System.Security.Cryptography;
using System.Text;

namespace DevStart.Domain.Admin
{
    /// <summary>
    /// What is left of a banned account after it is erased: a one-way hash of the email and the moment
    /// the ban stops mattering. Without it, "delete my account, then register again" would be a
    /// one-click way out of a ban — the erasure removes the very row (<c>users</c>) the ban lives on.
    ///
    /// The hash is not reversible and is never shown; it can only answer "is this exact address
    /// currently barred?" at registration. Nothing else about the person survives.
    /// </summary>
    public sealed class BannedIdentity : Entity
    {
        public Guid Id { get; set; }

        /// <summary>SHA-256 (hex) of the trimmed, lower-cased email. The address itself is not stored.</summary>
        public string EmailHash { get; set; } = null!;

        /// <summary>When the original ban expires; <c>null</c> for a permanent ban.</summary>
        public DateTime? BanExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsInForce(DateTime utcNow) => BanExpiresAt is null || BanExpiresAt > utcNow;

        public BannedIdentity()
        {
        }

        public static string HashEmail(string email)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()));
            return Convert.ToHexString(hash);
        }

        public static BannedIdentity Create(string email, DateTime? banExpiresAt, DateTime utcNow)
            => new()
            {
                Id = Guid.NewGuid(),
                EmailHash = HashEmail(email),
                BanExpiresAt = banExpiresAt,
                CreatedAt = utcNow,
            };
    }
}
