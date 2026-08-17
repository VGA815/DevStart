using FluentValidation;

namespace DevStart.Application.Admin.Valuation.SaveBenchmarkIndustryMapping
{
    internal sealed class SaveBenchmarkIndustryMappingCommandValidator
        : AbstractValidator<SaveBenchmarkIndustryMappingCommand>
    {
        public SaveBenchmarkIndustryMappingCommandValidator()
        {
            RuleFor(c => c.SourceKind).IsInEnum();

            RuleFor(c => c.ExternalKey)
                .NotEmpty()
                .WithMessage("An external key (Damodaran bucket name or ОКВЭД code) is required.")
                .MaximumLength(200)
                .WithMessage("An external key must be at most 200 characters.");

            RuleFor(c => c.Industry!.Value).IsInEnum().When(c => c.Industry.HasValue);

            RuleFor(c => c.Note).MaximumLength(512);
        }
    }
}
