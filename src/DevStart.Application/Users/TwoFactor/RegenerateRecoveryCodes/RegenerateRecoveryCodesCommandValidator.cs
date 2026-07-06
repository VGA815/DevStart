using FluentValidation;

namespace DevStart.Application.Users.TwoFactor.RegenerateRecoveryCodes
{
    internal sealed class RegenerateRecoveryCodesCommandValidator : AbstractValidator<RegenerateRecoveryCodesCommand>
    {
        public RegenerateRecoveryCodesCommandValidator()
        {
            RuleFor(c => c.Code).NotEmpty().MaximumLength(32);
        }
    }
}
