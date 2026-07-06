using FluentValidation;

namespace DevStart.Application.Admin.Users.ResetTwoFactor
{
    internal sealed class ResetUserTwoFactorCommandValidator : AbstractValidator<ResetUserTwoFactorCommand>
    {
        public ResetUserTwoFactorCommandValidator()
        {
            RuleFor(c => c.UserId).NotEmpty();
            RuleFor(c => c.Reason)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(1000);
        }
    }
}
