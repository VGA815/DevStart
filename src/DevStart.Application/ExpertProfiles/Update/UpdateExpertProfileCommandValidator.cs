using FluentValidation;

namespace DevStart.Application.ExpertProfiles.Update
{
    internal sealed class UpdateExpertProfileCommandValidator : AbstractValidator<UpdateExpertProfileCommand>
    {
        public UpdateExpertProfileCommandValidator()
        {
            RuleFor(x => x.Specializations)
                .NotNull()
                .Must(x => x.Count >= 1).WithMessage("At least one specialization is required.");
            RuleForEach(x => x.Specializations).IsInEnum();
        }
    }
}
