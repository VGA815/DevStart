using DevStart.Domain.StartupCompetitors;
using FluentValidation;

namespace DevStart.Application.StartupCompetitors.Create
{
    internal sealed class CreateStartupCompetitorCommandValidator : AbstractValidator<CreateStartupCompetitorCommand>
    {
        public CreateStartupCompetitorCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

            // The website is mandatory: it is what makes a competitor card checkable and what the
            // per-startup dedup key is derived from.
            RuleFor(x => x.Website)
                .NotEmpty()
                .MaximumLength(2000)
                .Must(w => StartupCompetitor.NormalizeDomain(w) is not null)
                .WithMessage("Website must be an absolute http(s) URL, e.g. https://competitor.com.");

            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
            RuleFor(x => x.StrengthsVsUs).MaximumLength(2000).When(x => x.StrengthsVsUs is not null);
            RuleFor(x => x.WeaknessesVsUs).MaximumLength(2000).When(x => x.WeaknessesVsUs is not null);
        }
    }
}
