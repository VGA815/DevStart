using DevStart.SharedKernel;

namespace DevStart.Domain.UserConsents
{
    public sealed class UserConsent : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public ConsentType Type { get; set; }
        public string DocumentVersion { get; set; } = null!;
        public DateTime AcceptedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public bool IsActive => RevokedAt is null;

        public UserConsent() { }

        public static UserConsent Create(
            Guid userId,
            ConsentType type,
            string documentVersion,
            DateTime acceptedAt)
            => new()
            {
                Id              = Guid.NewGuid(),
                UserId          = userId,
                Type            = type,
                DocumentVersion = documentVersion,
                AcceptedAt      = acceptedAt
            };

        public void Revoke(DateTime revokedAt)
        {
            RevokedAt = revokedAt;
        }
    }
}
