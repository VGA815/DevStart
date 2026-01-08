using FluentValidation;

namespace DevStart.Application.StartupMembers.Create
{
    internal sealed class CreateStartupMemberCommandValidator : AbstractValidator<CreateStartupMemberCommand>
    {
        public CreateStartupMemberCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.ProfileId).NotEmpty();
            RuleFor(x => x.IsPublic).NotEmpty();
            RuleFor(x => x.Role).NotEmpty().IsInEnum();
        }
    }
}
