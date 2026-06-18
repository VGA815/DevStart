using DevStart.Domain.Profiles;
using DevStart.SharedKernel;

namespace DevStart.Domain.Experts
{
    public sealed class ExpertProfile : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Personal data (name, bio, website, visibility, social links) lives on the shared Profile,
        // referenced by UserId. ExpertProfile only carries expert-specific data (specializations, experience).
        public Profile Profile { get; set; } = null!;

        public ExpertProfile()
        {
        }

        public static ExpertProfile Create(Guid userId, DateTime createdAt)
            => new()
            {
                Id = userId,
                UserId = userId,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

        public void Touch(DateTime updatedAt) => UpdatedAt = updatedAt;
    }
}
