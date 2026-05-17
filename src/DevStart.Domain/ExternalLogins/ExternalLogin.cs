using DevStart.SharedKernel;

namespace DevStart.Domain.ExternalLogins
{
    public sealed class ExternalLogin : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public ExternalLoginProvider Provider { get; set; }
        public string ProviderUserId { get; set; } = null!;
        public string? Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsedAt { get; set; }

        public ExternalLogin()
        {
        }

        public static ExternalLogin Create(
            Guid userId,
            ExternalLoginProvider provider,
            string providerUserId,
            string? email,
            DateTime now)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = provider,
                ProviderUserId = providerUserId,
                Email = email,
                CreatedAt = now,
                LastUsedAt = now,
            };

        public void Touch(DateTime now)
        {
            LastUsedAt = now;
        }
    }
}
