using FluentValidation;

namespace DevStart.Application.StartupMembers.Delete
{
    internal sealed class DeleteStartupMemberCommandValidator : AbstractValidator<DeleteStartupMemberCommand>
    {
        public DeleteStartupMemberCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.ProfileId).NotEmpty();
        }
    }
}
