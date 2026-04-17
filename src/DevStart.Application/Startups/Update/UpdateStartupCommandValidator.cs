using FluentValidation;

namespace DevStart.Application.Startups.Update
{
    internal sealed class UpdateStartupCommandValidator : AbstractValidator<UpdateStartupCommand>
    {
        public UpdateStartupCommandValidator()
        {
            RuleFor(s => s.Name).NotEmpty();
            RuleFor(s => s.PublicEmail).NotEmpty().EmailAddress();
            RuleFor(s => s.Stage).IsInEnum();
        }
    }
}
