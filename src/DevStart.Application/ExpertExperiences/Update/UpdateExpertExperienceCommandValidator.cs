using DevStart.SharedKernel;
using FluentValidation;

namespace DevStart.Application.ExpertExperiences.Update
{
    internal sealed class UpdateExpertExperienceCommandValidator : AbstractValidator<UpdateExpertExperienceCommand>
    {
        public UpdateExpertExperienceCommandValidator(IDateTimeProvider dateTimeProvider)
        {
            DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

            RuleFor(x => x.Id).NotEmpty();
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
