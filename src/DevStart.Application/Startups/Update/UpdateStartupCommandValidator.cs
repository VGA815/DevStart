using DevStart.Domain.Startups;
using FluentValidation;

namespace DevStart.Application.Startups.Update
{
    internal sealed class UpdateStartupCommandValidator : AbstractValidator<UpdateStartupCommand>
    {
        public UpdateStartupCommandValidator()
        {
            RuleFor(s => s.Name).NotEmpty();
            RuleFor(s => s.PublicEmail).NotEmpty().EmailAddress();
            RuleFor(s => s.Stage).IsInEnum();
            RuleFor(s => s.Tam).GreaterThanOrEqualTo(0).When(s => s.Tam.HasValue);
            RuleFor(s => s.Sam).GreaterThanOrEqualTo(0).When(s => s.Sam.HasValue);
            RuleFor(s => s.Som).GreaterThanOrEqualTo(0).When(s => s.Som.HasValue);
            RuleFor(s => s.MarketGrowthRate).GreaterThanOrEqualTo(0).When(s => s.MarketGrowthRate.HasValue);
            RuleFor(s => s.Location).IsInEnum();
            RuleFor(s => s.Industry).IsInEnum().When(s => s.Industry.HasValue);
            RuleFor(s => s.TargetRoundAmount).GreaterThanOrEqualTo(0).When(s => s.TargetRoundAmount.HasValue);

            // The check digit is a local, instant catch for a typo — worth doing before any external
            // lookup, and worth doing whether or not one is ever configured. An empty string is the
            // "clear it" case and skips the check.
            //
            // The wording is taken from the domain error rather than written again here. Both paths
            // are reachable — the validator rejects an HTTP caller, the handler rejects anyone calling
            // it directly — and two hand-written texts for one failure drift into two different
            // explanations of the same thing.
            RuleFor(s => s.Inn)
                .Must(RussianTaxId.IsValidInn)
                .WithMessage(StartupErrors.InvalidInn.Description)
                .When(s => !string.IsNullOrWhiteSpace(s.Inn));

            RuleFor(s => s.Ogrn)
                .Must(RussianTaxId.IsValidOgrn)
                .WithMessage(StartupErrors.InvalidOgrn.Description)
                .When(s => !string.IsNullOrWhiteSpace(s.Ogrn));
        }
    }
}
