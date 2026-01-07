using FluentValidation;

namespace DevStart.Application.StartupRoadmapItems.Update
{
    internal sealed class UpdateStartupRoadmapItemCommandValidator : AbstractValidator<UpdateStartupRoadmapItemCommand>
    {
        public UpdateStartupRoadmapItemCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.StartupStage).NotEmpty().IsInEnum();
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.TargetDate).NotEmpty();
            RuleFor(x => x.ItemId).NotEmpty();
            RuleFor(x => x.Status).NotEmpty().IsInEnum();
        }
    }
}
