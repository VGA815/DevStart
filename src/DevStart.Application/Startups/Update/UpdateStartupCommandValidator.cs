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
            RuleFor(s => s.Tam).GreaterThanOrEqualTo(0).When(s => s.Tam.HasValue);
            RuleFor(s => s.Sam).GreaterThanOrEqualTo(0).When(s => s.Sam.HasValue);
            RuleFor(s => s.Som).GreaterThanOrEqualTo(0).When(s => s.Som.HasValue);
        }
    }
}
