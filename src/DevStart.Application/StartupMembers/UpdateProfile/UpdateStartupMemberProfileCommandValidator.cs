using FluentValidation;

namespace DevStart.Application.StartupMembers.UpdateProfile
{
    internal sealed class UpdateStartupMemberProfileCommandValidator : AbstractValidator<UpdateStartupMemberProfileCommand>
    {
        public UpdateStartupMemberProfileCommandValidator()
        {
            RuleFor(x => x.StartupId).NotEmpty();
            RuleFor(x => x.Position).IsInEnum().When(x => x.Position.HasValue);
            RuleFor(x => x.YearsOfExperience).GreaterThanOrEqualTo(0).When(x => x.YearsOfExperience.HasValue);
            RuleFor(x => x.PreviousStartupsCount).GreaterThanOrEqualTo(0).When(x => x.PreviousStartupsCount.HasValue);
        }
    }
}
