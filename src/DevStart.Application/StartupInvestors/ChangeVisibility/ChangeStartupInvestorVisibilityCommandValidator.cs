using FluentValidation;

namespace DevStart.Application.StartupInvestors.ChangeVisibility
{
    internal sealed class ChangeStartupInvestorVisibilityCommandValidator : AbstractValidator<ChangeStartupInvestorVisibilityCommand>
    {
        public ChangeStartupInvestorVisibilityCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.IsPublic).NotEmpty();
        }
    }
}
