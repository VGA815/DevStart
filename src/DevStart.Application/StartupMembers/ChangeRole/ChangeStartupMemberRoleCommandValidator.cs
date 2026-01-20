using FluentValidation;

namespace DevStart.Application.StartupMembers.ChangeRole
{
    internal sealed class ChangeStartupMemberRoleCommandValidator : AbstractValidator<ChangeStartupMemberRoleCommand>
    {
        public ChangeStartupMemberRoleCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.ProfileId).NotEmpty();
            RuleFor(x => x.Role).IsInEnum();
        }
    }
}
