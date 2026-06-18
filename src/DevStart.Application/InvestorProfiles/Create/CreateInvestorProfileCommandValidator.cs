using FluentValidation;

namespace DevStart.Application.InvestorProfiles.Create
{
    internal sealed class CreateInvestorProfileCommandValidator : AbstractValidator<CreateInvestorProfileCommand>
    {
        public CreateInvestorProfileCommandValidator()
        {
            RuleFor(x => x.Type).IsInEnum();
        }
    }
}
