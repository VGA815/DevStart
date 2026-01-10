using FluentValidation;

namespace DevStart.Application.StartupFollowers.Delete
{
    internal sealed class DeleteStartupFollowerCommandValidator : AbstractValidator<DeleteStartupFollowerCommand>
    {
        public DeleteStartupFollowerCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
        }
    }
}
