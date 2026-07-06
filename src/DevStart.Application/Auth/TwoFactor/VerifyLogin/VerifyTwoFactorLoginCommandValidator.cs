using FluentValidation;

namespace DevStart.Application.Auth.TwoFactor.VerifyLogin
{
    internal sealed class VerifyTwoFactorLoginCommandValidator : AbstractValidator<VerifyTwoFactorLoginCommand>
    {
        public VerifyTwoFactorLoginCommandValidator()
        {
            RuleFor(c => c.PendingToken).NotEmpty();
            RuleFor(c => c.Code).NotEmpty().MaximumLength(32);
        }
    }
}
