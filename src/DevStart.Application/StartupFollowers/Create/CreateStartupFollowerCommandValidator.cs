using FluentValidation;

namespace DevStart.Application.StartupFollowers.Create
{
    internal sealed class CreateStartupFollowerCommandValidator : AbstractValidator<CreateStartupFollowerCommand>
    {
        public CreateStartupFollowerCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.ProfileId).NotEmpty();
        }
    }
}
