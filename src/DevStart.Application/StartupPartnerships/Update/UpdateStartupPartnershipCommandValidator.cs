using DevStart.Domain.StartupPartnerships;
using FluentValidation;

namespace DevStart.Application.StartupPartnerships.Update
{
    internal sealed class UpdateStartupPartnershipCommandValidator
        : AbstractValidator<UpdateStartupPartnershipCommand>
    {
        public UpdateStartupPartnershipCommandValidator()
        {
            RuleFor(x => x.PartnershipId).NotEmpty();
            RuleFor(x => x.PartnerName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Kind).IsInEnum();

            RuleFor(x => x.Website)
                .NotEmpty()
                .MaximumLength(2000)
                .Must(w => StartupPartnership.NormalizeDomain(w) is not null)
                .WithMessage("Website must be an absolute http(s) URL, e.g. https://partner.com.");

            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        }
    }
}
