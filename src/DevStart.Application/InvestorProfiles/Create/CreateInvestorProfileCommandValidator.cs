using FluentValidation;

namespace DevStart.Application.InvestorProfiles.Create
{
    internal sealed class CreateInvestorProfileCommandValidator : AbstractValidator<CreateInvestorProfileCommand>
    {
        public CreateInvestorProfileCommandValidator()
        {
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Bio).MaximumLength(2000);
            RuleFor(x => x.Website).MaximumLength(500);
        }
    }
}
