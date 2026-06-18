using DevStart.Domain.Profiles;
using DevStart.SharedKernel;

namespace DevStart.Domain.Investors
{
    public sealed class InvestorProfile : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public InvestorProfileType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Personal data (name, bio, website, visibility) lives on the shared Profile, referenced by UserId.
        // InvestorProfile only carries investor-specific data (Type).
        public Profile Profile { get; set; } = null!;

        public InvestorProfile()
        {
        }

        public static InvestorProfile Create(
            Guid userId,
            InvestorProfileType type,
            DateTime createdAt)
            => new()
            {
                Id = userId,
                UserId = userId,
                Type = type,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

        public void Update(
            InvestorProfileType type,
            DateTime updatedAt)
        {
            Type = type;
            UpdatedAt = updatedAt;
        }
    }
}
