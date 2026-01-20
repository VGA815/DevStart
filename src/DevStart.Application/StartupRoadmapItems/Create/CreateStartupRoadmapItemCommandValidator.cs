using FluentValidation;

namespace DevStart.Application.StartupRoadmapItems.Create
{
    internal sealed class CreateStartupRoadmapItemCommandValidator : AbstractValidator<CreateStartupRoadmapItemCommand>
    {
        public CreateStartupRoadmapItemCommandValidator()
        {
            RuleFor(sri => sri.Title).NotEmpty();
            RuleFor(sri => sri.StartupId).NotEmpty();
            RuleFor(sri => sri.StartupStage).IsInEnum();
            RuleFor(sri => sri.Status).IsInEnum();
        }
    }
}
