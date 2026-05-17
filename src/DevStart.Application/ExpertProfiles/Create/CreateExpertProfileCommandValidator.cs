using FluentValidation;

namespace DevStart.Application.ExpertProfiles.Create
{
    internal sealed class CreateExpertProfileCommandValidator : AbstractValidator<CreateExpertProfileCommand>
    {
        public CreateExpertProfileCommandValidator()
        {
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Bio).MaximumLength(2000);
            RuleFor(x => x.Website).MaximumLength(500);
            RuleFor(x => x.LinkedInUrl).MaximumLength(500);
            RuleFor(x => x.TwitterUrl).MaximumLength(500);
            RuleFor(x => x.GitHubUrl).MaximumLength(500);
            RuleFor(x => x.TelegramUrl).MaximumLength(500);
            RuleFor(x => x.Specializations)
                .NotNull()
                .Must(x => x.Count >= 1).WithMessage("At least one specialization is required.");
            RuleForEach(x => x.Specializations).IsInEnum();
        }
    }
}
