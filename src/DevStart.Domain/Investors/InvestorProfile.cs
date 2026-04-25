using DevStart.SharedKernel;

namespace DevStart.Domain.Investors
{
    public sealed class InvestorProfile : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public InvestorProfileType Type { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public InvestorProfile()
        {
        }

        public static InvestorProfile Create(
            Guid userId,
            InvestorProfileType type,
            string displayName,
            string? bio,
            string? website,
            bool isPublic,
            DateTime createdAt)
            => new()
            {
                Id = userId,
                UserId = userId,
                Type = type,
                DisplayName = displayName,
                Bio = bio,
                Website = website,
                IsPublic = isPublic,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

        public void Update(
            InvestorProfileType type,
            string displayName,
            string? bio,
            string? website,
            bool isPublic,
            DateTime updatedAt)
        {
            Type = type;
            DisplayName = displayName;
            Bio = bio;
            Website = website;
            IsPublic = isPublic;
            UpdatedAt = updatedAt;
        }
    }
}
