using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ExpertExperiences.Update
{
    public sealed class UpdateExpertExperienceCommand : ICommand
    {
        public Guid Id { get; set; }
        public string Company { get; set; } = null!;
        public string Position { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Description { get; set; }

        public UpdateExpertExperienceCommand(
            Guid id,
            string company,
            string position,
            DateOnly startDate,
            DateOnly? endDate,
            string? description)
        {
            Id = id;
            Company = company;
            Position = position;
            StartDate = startDate;
            EndDate = endDate;
            Description = description;
        }
    }
}
