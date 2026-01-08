using FluentValidation;

namespace DevStart.Application.StartupMembers.ChangeVisibility
{
    internal sealed class ChangeStartupMemberVisibilityCommandValidator : AbstractValidator<ChangeStartupMemberVisibilityCommand>
    {
        public ChangeStartupMemberVisibilityCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.IsPublic).NotEmpty();
            RuleFor(x => x.ProfileId).NotEmpty();
        }
    }
}
