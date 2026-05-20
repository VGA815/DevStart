using FluentValidation;

namespace DevStart.Application.ExpertCollaborationRequests.Create
{
    internal sealed class CreateExpertCollaborationRequestCommandValidator : AbstractValidator<CreateExpertCollaborationRequestCommand>
    {
        public CreateExpertCollaborationRequestCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.CollaborationType).IsInEnum();
            RuleFor(x => x.Message).MaximumLength(2000);
            RuleFor(x => x.ProposedHoursPerWeek)
                .InclusiveBetween(1, 168)
                .When(x => x.ProposedHoursPerWeek.HasValue);
            RuleFor(x => x.ProposedRate)
                .GreaterThan(0m)
                .When(x => x.ProposedRate.HasValue);
        }
    }
}
