using FluentValidation;

namespace DevStart.Application.Auth.TwoFactor.ConfirmSetupLogin
{
    internal sealed class ConfirmTwoFactorSetupLoginCommandValidator : AbstractValidator<ConfirmTwoFactorSetupLoginCommand>
    {
        public ConfirmTwoFactorSetupLoginCommandValidator()
        {
            RuleFor(c => c.PendingToken).NotEmpty();
            RuleFor(c => c.Code).NotEmpty().MaximumLength(32);
        }
    }
}
