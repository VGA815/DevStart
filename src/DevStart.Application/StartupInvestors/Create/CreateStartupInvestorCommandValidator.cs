using FluentValidation;

namespace DevStart.Application.StartupInvestors.Create
{
    internal sealed class CreateStartupInvestorCommandValidator : AbstractValidator<CreateStartupInvestorCommand>
    {
        public CreateStartupInvestorCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.IsPublic).NotEmpty();
            RuleFor(x => x.ProfileId).NotEmpty();
        }
    }
}
