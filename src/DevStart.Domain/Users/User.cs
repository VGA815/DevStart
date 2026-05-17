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

        public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

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
