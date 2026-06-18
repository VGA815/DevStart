using FluentValidation;

namespace DevStart.Application.ExpertProfiles.Create
{
    internal sealed class CreateExpertProfileCommandValidator : AbstractValidator<CreateExpertProfileCommand>
    {
        public CreateExpertProfileCommandValidator()
        {
            RuleFor(x => x.Specializations)
                .NotNull()
                .Must(x => x.Count >= 1).WithMessage("At least one specialization is required.");
            RuleForEach(x => x.Specializations).IsInEnum();
        }
    }
}
