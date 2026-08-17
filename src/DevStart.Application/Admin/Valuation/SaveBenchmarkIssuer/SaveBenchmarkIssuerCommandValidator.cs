using FluentValidation;

namespace DevStart.Application.Admin.Valuation.SaveBenchmarkIssuer
{
    internal sealed class SaveBenchmarkIssuerCommandValidator : AbstractValidator<SaveBenchmarkIssuerCommand>
    {
        public SaveBenchmarkIssuerCommandValidator()
        {
            // Each validator gets its own message: FluentValidation attaches WithMessage to the rule
            // immediately preceding it, so a chain with one trailing message leaves the other failure
            // modes on the framework default.
            RuleFor(c => c.Ticker)
                .NotEmpty()
                .WithMessage("A MOEX ticker is required.")
                .MaximumLength(16)
                .WithMessage("A ticker must be at most 16 characters.")
                .Matches("^[A-Za-z0-9]+$")
                .WithMessage("Ticker must be a MOEX SECID (letters and digits only).");

            RuleFor(c => c.DisplayName).NotEmpty().MaximumLength(200);

            RuleFor(c => c.Industry).IsInEnum();

            // ГИР БО is queried by INN; a malformed one silently returns nothing, so reject it here.
            RuleFor(c => c.Inn!)
                .Matches("^[0-9]{10}$|^[0-9]{12}$")
                .WithMessage("INN must be 10 digits (legal entity) or 12 digits (sole trader).")
                .When(c => !string.IsNullOrWhiteSpace(c.Inn));

            RuleFor(c => c.RevenueOverride!.Value)
                .GreaterThan(0)
                .WithMessage("A revenue override must be positive.")
                .When(c => c.RevenueOverride.HasValue);

            // An override without a year and a reason is an unattributable number, and it is precisely
            // the number that overrides the automatic one — so both are mandatory alongside it.
            RuleFor(c => c.RevenueOverrideFiscalYear)
                .NotNull()
                .WithMessage("A revenue override must state the fiscal year it belongs to.")
                .InclusiveBetween(2000, 2100)
                .WithMessage("The fiscal year of a revenue override must be between 2000 and 2100.")
                .When(c => c.RevenueOverride.HasValue);

            RuleFor(c => c.RevenueOverrideNote)
                .NotEmpty()
                .WithMessage("A revenue override must say where the consolidated figure came from.")
                .MaximumLength(512)
                .WithMessage("The source note of a revenue override must be at most 512 characters.")
                .When(c => c.RevenueOverride.HasValue);

            RuleFor(c => c.Note).MaximumLength(512);
        }
    }
}
