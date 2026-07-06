using FluentValidation;

namespace DevStart.Application.Auth.TwoFactor.SetupLogin
{
    internal sealed class SetupTwoFactorLoginCommandValidator : AbstractValidator<SetupTwoFactorLoginCommand>
    {
        public SetupTwoFactorLoginCommandValidator()
        {
            RuleFor(c => c.PendingToken).NotEmpty();
        }
    }
}
