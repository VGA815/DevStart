using DevStart.SharedKernel;

namespace DevStart.Domain.Experts
{
    public sealed class ExpertExperience : Entity
    {
        public Guid Id { get; set; }
        public Guid ExpertProfileId { get; set; }
        public string Company { get; set; } = null!;
        public string Position { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ExpertExperience()
        {
        }

        public static ExpertExperience Create(
            Guid expertProfileId,
            string company,
            string position,
            DateOnly startDate,
            DateOnly? endDate,
            string? description,
            DateTime createdAt)
            => new()
            {
                Id = Guid.NewGuid(),
                ExpertProfileId = expertProfileId,
                Company = company,
                Position = position,
                StartDate = startDate,
                EndDate = endDate,
                Description = description,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

        public void Update(
            string company,
            string position,
            DateOnly startDate,
            DateOnly? endDate,
            string? description,
            DateTime updatedAt)
        {
            Company = company;
            Position = position;
            StartDate = startDate;
            EndDate = endDate;
            Description = description;
            UpdatedAt = updatedAt;
        }
    }
}
