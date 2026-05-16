using DevStart.Domain.UserConsents;

namespace DevStart.Application.UserConsents.GetConsents
{
    public sealed class UserConsentResponse
    {
        public ConsentType Type { get; set; }
        public string DocumentVersion { get; set; } = null!;
        public DateTime AcceptedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsMandatory { get; set; }
    }
}
