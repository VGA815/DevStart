using FluentValidation;

namespace DevStart.Application.StartupCompetitors.Update
{
    internal sealed class UpdateStartupCompetitorCommandValidator : AbstractValidator<UpdateStartupCompetitorCommand>
    {
        public UpdateStartupCompetitorCommandValidator()
        {
            RuleFor(x => x.CompetitorId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Website).MaximumLength(2000).When(x => x.Website is not null);
            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
            RuleFor(x => x.StrengthsVsUs).MaximumLength(2000).When(x => x.StrengthsVsUs is not null);
            RuleFor(x => x.WeaknessesVsUs).MaximumLength(2000).When(x => x.WeaknessesVsUs is not null);
        }
    }
}
