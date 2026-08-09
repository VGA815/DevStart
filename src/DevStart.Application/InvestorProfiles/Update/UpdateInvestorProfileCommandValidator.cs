using FluentValidation;

namespace DevStart.Application.InvestorProfiles.Update
{
    internal sealed class UpdateInvestorProfileCommandValidator : AbstractValidator<UpdateInvestorProfileCommand>
    {
        public UpdateInvestorProfileCommandValidator()
        {
            RuleFor(x => x.Type).IsInEnum();

            // Same bounds as the dashboard form and the Profile columns these land in.
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Bio).MaximumLength(2000);
            RuleFor(x => x.Website).MaximumLength(500);
        }
    }
}
