using DevStart.SharedKernel;

namespace DevStart.Domain.Users
{
    public sealed class User : Entity
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool IsVerified { get; set; }
        public string? PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public UserSystemRole Role { get; set; }

        public bool IsBanned { get; set; }
        public string? BanReason { get; set; }
        public DateTime? BannedAt { get; set; }
        public DateTime? BanExpiresAt { get; set; }
        public Guid? BannedByUserId { get; set; }

        public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

        /// <summary>
        /// True while a ban is in force. A temporary ban whose <see cref="BanExpiresAt"/> has passed is
        /// treated as lifted even before the ban-expiry job clears the flag ("lazy" expiry).
        /// </summary>
        public bool IsCurrentlyBanned(DateTime utcNow) =>
            IsBanned && (BanExpiresAt is null || BanExpiresAt > utcNow);

        public Result Ban(string reason, DateTime? expiresAt, Guid byUserId, DateTime utcNow)
        {
            if (IsCurrentlyBanned(utcNow))
            {
                return Result.Failure(UserErrors.AlreadyBanned);
            }
            if (expiresAt is not null && expiresAt <= utcNow)
            {
                return Result.Failure(UserErrors.BanExpiryInPast);
            }

            IsBanned = true;
            BanReason = reason;
            BannedAt = utcNow;
            BanExpiresAt = expiresAt;
            BannedByUserId = byUserId;
            UpdatedAt = utcNow;

            Raise(new UserBannedDomainEvent(Id, reason, expiresAt));
            return Result.Success();
        }

        public Result Unban(DateTime utcNow)
        {
            if (!IsBanned)
            {
                return Result.Failure(UserErrors.NotBanned);
            }

            IsBanned = false;
            BanReason = null;
            BannedAt = null;
            BanExpiresAt = null;
            BannedByUserId = null;
            UpdatedAt = utcNow;

            Raise(new UserUnbannedDomainEvent(Id));
            return Result.Success();
        }

        public static User Create(string username, string email, string passwordHash, DateTime createdAt)
        {
            return new User()
            {
                CreatedAt = createdAt,
                Email = email,
                Id = Guid.NewGuid(),
                IsVerified = false,
                PasswordHash = passwordHash,
                UpdatedAt = createdAt,
                Username = username,
                Role = UserSystemRole.User,
            };
        }

        public static User CreateExternal(string username, string email, bool emailVerified, DateTime createdAt)
        {
            return new User()
            {
                CreatedAt = createdAt,
                Email = email,
                Id = Guid.NewGuid(),
                IsVerified = emailVerified,
                PasswordHash = null,
                UpdatedAt = createdAt,
                Username = username,
                Role = UserSystemRole.User,
            };
        }

        public User()
        {

        }
    }
}
