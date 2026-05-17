using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ExpertExperiences.Delete
{
    public sealed class DeleteExpertExperienceCommand : ICommand
    {
        public Guid Id { get; set; }

        public DeleteExpertExperienceCommand(Guid id)
        {
            Id = id;
        }
    }
}
