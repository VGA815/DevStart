using FluentValidation;

namespace DevStart.Application.InvestorProfiles.Update
{
    internal sealed class UpdateInvestorProfileCommandValidator : AbstractValidator<UpdateInvestorProfileCommand>
    {
        public UpdateInvestorProfileCommandValidator()
        {
            RuleFor(x => x.Type).IsInEnum();
        }
    }
}
