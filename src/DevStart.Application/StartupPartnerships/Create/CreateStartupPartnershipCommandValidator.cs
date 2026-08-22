using DevStart.Domain.StartupPartnerships;
using FluentValidation;

namespace DevStart.Application.StartupPartnerships.Create
{
    internal sealed class CreateStartupPartnershipCommandValidator
        : AbstractValidator<CreateStartupPartnershipCommand>
    {
        public CreateStartupPartnershipCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.PartnerName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Kind).IsInEnum();

            // The website is mandatory: it is what lets a reader go and check the partner exists, and
            // it is where the per-startup dedup key comes from.
            RuleFor(x => x.Website)
                .NotEmpty()
                .MaximumLength(2000)
                .Must(w => StartupPartnership.NormalizeDomain(w) is not null)
                .WithMessage("Website must be an absolute http(s) URL, e.g. https://partner.com.");

            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        }
    }
}
