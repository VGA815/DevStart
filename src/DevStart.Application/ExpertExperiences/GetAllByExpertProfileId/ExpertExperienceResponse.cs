namespace DevStart.Application.ExpertExperiences.GetAllByExpertProfileId
{
    public sealed class ExpertExperienceResponse
    {
        public Guid Id { get; init; }
        public Guid ExpertProfileId { get; init; }
        public string Company { get; init; } = null!;
        public string Position { get; init; } = null!;
        public DateOnly StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        public string? Description { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
