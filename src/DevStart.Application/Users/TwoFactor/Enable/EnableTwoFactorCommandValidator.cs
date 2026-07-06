using FluentValidation;

namespace DevStart.Application.Users.TwoFactor.Enable
{
    internal sealed class EnableTwoFactorCommandValidator : AbstractValidator<EnableTwoFactorCommand>
    {
        public EnableTwoFactorCommandValidator()
        {
            RuleFor(c => c.Code).NotEmpty().MaximumLength(32);
        }
    }
}
