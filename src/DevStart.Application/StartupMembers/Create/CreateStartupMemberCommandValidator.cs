using FluentValidation;

namespace DevStart.Application.StartupMembers.Create
{
    internal sealed class CreateStartupMemberCommandValidator : AbstractValidator<CreateStartupMemberCommand>
    {
        public CreateStartupMemberCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.ProfileId).NotEmpty();
            RuleFor(x => x.Role).IsInEnum();
            RuleFor(x => x.Position).IsInEnum().When(x => x.Position.HasValue);
            RuleFor(x => x.YearsOfExperience).GreaterThanOrEqualTo(0).When(x => x.YearsOfExperience.HasValue);
            RuleFor(x => x.PreviousStartupsCount).GreaterThanOrEqualTo(0).When(x => x.PreviousStartupsCount.HasValue);
        }
    }
}
