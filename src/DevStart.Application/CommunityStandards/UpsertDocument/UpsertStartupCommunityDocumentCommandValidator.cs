using FluentValidation;

namespace DevStart.Application.CommunityStandards.UpsertDocument
{
    internal sealed class UpsertStartupCommunityDocumentCommandValidator
        : AbstractValidator<UpsertStartupCommunityDocumentCommand>
    {
        public UpsertStartupCommunityDocumentCommandValidator()
        {
            RuleFor(c => c.StartupId).NotEmpty();
            RuleFor(c => c.Type).IsInEnum();
            RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
            RuleFor(c => c.Content).NotEmpty().MaximumLength(100_000);
        }
    }
}
