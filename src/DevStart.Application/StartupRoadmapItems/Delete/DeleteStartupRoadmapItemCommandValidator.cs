using FluentValidation;

namespace DevStart.Application.StartupRoadmapItems.Delete
{
    internal sealed class DeleteStartupRoadmapItemCommandValidator : AbstractValidator<DeleteStartupRoadmapItemCommand>
    {
        public DeleteStartupRoadmapItemCommandValidator()
        {
            RuleFor(x => x.ItemId).NotEmpty();
        }
    }
}
