using DevStart.Domain.StartupCompetitors;
using FluentValidation;

namespace DevStart.Application.StartupCompetitors.Update
{
    internal sealed class UpdateStartupCompetitorCommandValidator : AbstractValidator<UpdateStartupCompetitorCommand>
    {
        public UpdateStartupCompetitorCommandValidator()
        {
            RuleFor(x => x.CompetitorId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

            // Mandatory on update too: a legacy card without a website has to gain one to be edited,
            // which is how those rows acquire their dedup key.
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
