using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Experts;

namespace DevStart.Application.ExpertProfiles.Update
{
    public sealed class UpdateExpertProfileCommand : ICommand
    {
        public List<ExpertSpecialization> Specializations { get; set; } = new();

        public UpdateExpertProfileCommand(List<ExpertSpecialization> specializations)
        {
            Specializations = specializations;
        }
    }
}
