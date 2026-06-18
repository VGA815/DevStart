using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Experts;

namespace DevStart.Application.ExpertProfiles.Create
{
    public sealed class CreateExpertProfileCommand : ICommand<Guid>
    {
        public List<ExpertSpecialization> Specializations { get; set; } = new();

        public CreateExpertProfileCommand(List<ExpertSpecialization> specializations)
        {
            Specializations = specializations;
        }
    }
}
