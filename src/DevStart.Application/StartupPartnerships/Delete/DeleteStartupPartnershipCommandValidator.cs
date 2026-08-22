using FluentValidation;

namespace DevStart.Application.StartupPartnerships.Delete
{
    internal sealed class DeleteStartupPartnershipCommandValidator
        : AbstractValidator<DeleteStartupPartnershipCommand>
    {
        public DeleteStartupPartnershipCommandValidator()
        {
            RuleFor(x => x.PartnershipId).NotEmpty();
        }
    }
}
