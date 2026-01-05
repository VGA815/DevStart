using DevStart.SharedKernel;

namespace DevStart.Domain.Users
{
    public sealed class User : Entity
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User(string email, string username, string passwordHash, IDateTimeProvider dateTimeProvider)
        {
            Id = Guid.NewGuid();
            Username = username;
            Email = email;
            PasswordHash = passwordHash;
            CreatedAt = dateTimeProvider.UtcNow;
            UpdatedAt = dateTimeProvider.UtcNow;
        }
    }
}
