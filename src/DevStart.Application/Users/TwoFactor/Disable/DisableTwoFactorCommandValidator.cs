using FluentValidation;

namespace DevStart.Application.Users.TwoFactor.Disable
{
    internal sealed class DisableTwoFactorCommandValidator : AbstractValidator<DisableTwoFactorCommand>
    {
        public DisableTwoFactorCommandValidator()
        {
            RuleFor(c => c.Code).NotEmpty().MaximumLength(32);
        }
    }
}
