using DevStart.SharedKernel;
using FluentValidation;

namespace DevStart.Application.ExpertExperiences.Create
{
    internal sealed class CreateExpertExperienceCommandValidator : AbstractValidator<CreateExpertExperienceCommand>
    {
        public CreateExpertExperienceCommandValidator(IDateTimeProvider dateTimeProvider)
        {
            DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

            RuleFor(x => x.ExpertProfileId).NotEmpty();
            RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Position).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(today).WithMessage("StartDate must not be in the future.");
            RuleFor(x => x)
                .Must(x => !x.EndDate.HasValue || x.StartDate <= x.EndDate.Value)
                .WithMessage("StartDate must be less than or equal to EndDate.")
                .WithName("EndDate");
        }
    }
}
