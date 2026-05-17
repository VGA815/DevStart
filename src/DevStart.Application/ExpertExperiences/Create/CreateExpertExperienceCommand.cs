using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ExpertExperiences.Create
{
    public sealed class CreateExpertExperienceCommand : ICommand<Guid>
    {
        public Guid ExpertProfileId { get; set; }
        public string Company { get; set; } = null!;
        public string Position { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Description { get; set; }

        public CreateExpertExperienceCommand(
            Guid expertProfileId,
            string company,
            string position,
            DateOnly startDate,
            DateOnly? endDate,
            string? description)
        {
            ExpertProfileId = expertProfileId;
            Company = company;
            Position = position;
            StartDate = startDate;
            EndDate = endDate;
            Description = description;
        }
    }
}
