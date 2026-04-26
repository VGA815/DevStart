using FluentValidation;

namespace DevStart.Application.StartupCompetitors.Delete
{
    internal sealed class DeleteStartupCompetitorCommandValidator : AbstractValidator<DeleteStartupCompetitorCommand>
    {
        public DeleteStartupCompetitorCommandValidator()
        {
            RuleFor(x => x.CompetitorId).NotEmpty();
        }
    }
}
